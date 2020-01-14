using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FileManagerApp.Entities;
using FileManagerApp.Models;

namespace FileManagerApp.Controllers {
    public class MainController : Controller {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private string RootPath = "~/File-Repository/";
        private List<FileItem> Items;

        public MainController() {
        }

        // GET: FileManager/Main
        public ActionResult Index() {
            return View();
        }

        public string GetChildId(string input, char delimitter)
        {
            string[] words = input.Split(new[] { delimitter }, StringSplitOptions.RemoveEmptyEntries);
            words = words.Reverse().ToArray();
            return words[0].ToString();
        }

        [HttpGet]
        public async Task<ActionResult> Update(string path) {

            string strparse = (path == "" ? null : (path.Contains("|") ? GetChildId(path,'|') : path ));
            int? id = (string.IsNullOrEmpty(strparse) ? (int?)null : Convert.ToInt32(strparse));                

            // get current files & folders
            var items = db.FileItems.Where(x => x.FileId == id).OrderByDescending(x => x.IsFolder).Select(x => new FileItemModel {
                Id = x.Id,
                Name = x.Name,
                Path = x.Path,
                MimeType = x.MimeType,
                CDate = x.CDate,
                MDate = x.MDate,
                IsFolder = x.IsFolder,
                FileId = x.FileId
            });

            return Json(new OperationResult {
                Status = OperationStats.Success,
                Items = await items.ToListAsync()
            }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult TreeView()
        {
            FileItemTreeNode _treenode = GetTreeVeiwList();
            return Json(_treenode, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateFileItemModel model) {
            if (!ModelState.IsValid) {
                return Json(new OperationResult {
                    Status = OperationStats.Error,
                    Errors = GetErrors(ModelState)
                });
            }

            model.Path = model.Path.Trim('/');
            var absPath = Server.MapPath(string.Concat(model.Path.Replace("ROOT", RootPath), "/", model.Name));
            var created = false;
            try {
                if (model.IsFolder) {
                    if (!Directory.Exists(absPath)) {
                        Directory.CreateDirectory(absPath);
                        created = true;
                    }
                } else {
                    if (!System.IO.File.Exists(absPath)) {
                        System.IO.File.WriteAllBytes(absPath, new byte[0]);
                        created = true;
                    }
                }

                if (created) {
                    // add to database
                    FileItem newEntity = new FileItem {
                        Name = model.Name,
                        MimeType = model.Name.Contains('.') ? model.Name.Split('.').LastOrDefault() : null,
                        Path = model.Path,
                        IsFolder = model.IsFolder,
                        CDate = DateTime.UtcNow,
                        MDate = DateTime.UtcNow,
                    };

                    if (await db.FileItems.AnyAsync()) {
                        var p = string.Join("/", model.Path.Split('/').Reverse().Skip(1).Reverse().ToArray());
                        FileItem parent = await db.FileItems
                            .FirstOrDefaultAsync(x => x.Path.Equals(p));
                        if (parent != null)
                            newEntity.FileId = parent.Id;
                    }

                    db.FileItems.Add(newEntity);
                }
            } catch (Exception ex) {
                throw;
            }

            if (await db.SaveChangesAsync() > 0) {
                return Json(new OperationResult {
                    Status = OperationStats.Success,
                    Message = StringResources.SuccessfullyCreated
                });
            }

            return Json(new OperationResult {
                Status = OperationStats.Error,
                Message = StringResources.UnknownErrorOccurred
            });
        }

        [HttpPost]
        public async Task<ActionResult> Upload(UploadFileItemModel model) {
            if (!ModelState.IsValid) {
                return Json(new OperationResult {
                    Status = OperationStats.Error,
                    Errors = GetErrors(ModelState)
                });
            }

            model.Path = model.Path.Trim('/');
            List<FileItem> listToAdd = new List<FileItem>();
            //var rootPath = model.Path.TrimEnd('/').Remove(model.Path.LastIndexOf('/'));
            //FileItem sibling = await db.FileItems.FirstOrDefaultAsync(x => x.Path.Equals(rootPath));

            try {
                var absPath = Server.MapPath(string.Concat(model.Path.Replace("ROOT", RootPath), "/", model.PostedFile.FileName));
                if (System.IO.File.Exists(absPath)) {
                    return Json(new OperationResult {
                        Status = OperationStats.Error,
                        Message = string.Format(StringResources.ItemAlreadyExists, model.PostedFile.FileName)
                    });
                }

                model.PostedFile.SaveAs(absPath);
                listToAdd.Add(new FileItem {
                    Name = model.PostedFile.FileName,
                    MimeType = model.PostedFile.ContentType,
                    Path = model.Path.Trim('/'),
                    CDate = DateTime.UtcNow,
                    MDate = DateTime.UtcNow,
                    FileId = model.Id
                });

                if (!listToAdd.Any())
                    return Json(new OperationResult {
                        Status = OperationStats.Error,
                        Message = StringResources.UnknownErrorOccurred
                    });

                db.FileItems.AddRange(listToAdd);
                await db.SaveChangesAsync();

                return Json(new OperationResult {
                    Status = OperationStats.Success,
                    Message = string.Format(StringResources.SuccessfullyUploaded, model.PostedFile.FileName)
                });
            } catch (Exception ex) {
                throw;
            }
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id) {
            try {
                FileItem file = await db.FileItems.FindAsync(id);

                if (file == null) {
                    return RedirectToAction("Index");
                }
                string path = Server.MapPath(string.Concat(file.Path.Replace("ROOT", RootPath), '/', file.Name));

                if (!System.IO.File.Exists(path)) {
                    return RedirectToAction("Index");
                }

                var result = new EditFileItemModel {
                    Id = file.Id,
                    Path = file.Path,
                    Name = file.Name,
                    CDate = file.CDate,
                    MDate = file.MDate
                };

                using (StreamReader sr = new StreamReader(path)) {
                    result.Content = await sr.ReadToEndAsync();
                }

                return View(result);
            } catch (Exception ex) {
                throw;
            }
        }

        [HttpPost]
        public async Task<ActionResult> Rename(EditFileItemModel model) {
            try {
                FileItem file = await db.FileItems.FindAsync(model.Id);

                if (file == null) {
                    return Json(new OperationResult {
                        Status = OperationStats.Error,
                        Message = StringResources.NotFoundInDatabase,
                    });
                }

                string absPath = Server.MapPath(string.Concat(file.Path.Replace("ROOT", RootPath), '/', file.Name));
                if (model.IsFolder) {
                    if (!Directory.Exists(absPath)) {
                        return Json(new OperationResult {
                            Status = OperationStats.Error,
                            Message = StringResources.NotFoundInFileSystem,
                        });
                    }
                    // Rename the name of current directory
                    // Rename in File System
                    Directory.Move(absPath, RenameFileOrDirectory(absPath, model.Name));
                    // Rename in Database
                    file.Name = model.Name;
                    file.MDate = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    // Change sub directory and file pathes
                    await UpdateSubDirectoryPath(await db.FileItems.Where(x => x.FileId != null && x.FileId == file.Id).ToListAsync());
                } else {
                    if (!System.IO.File.Exists(absPath)) {
                        return Json(new OperationResult {
                            Status = OperationStats.Error,
                            Message = StringResources.NotFoundInFileSystem,
                        });
                    }
                    // Rename in File System
                    System.IO.File.Move(absPath, RenameFileOrDirectory(absPath, model.Name));

                    // Rename in Database
                    file.Name = model.Name;
                    file.MDate = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                await db.SaveChangesAsync();

                return Json(new OperationResult {
                    Status = OperationStats.Success,
                    Message = StringResources.NameChanged,
                });
            } catch (Exception ex) {
                throw;
            }
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id) {
            try {
                FileItem file = await db.FileItems.FindAsync(id);

                if (file == null) {
                    return Json(new OperationResult {
                        Status = OperationStats.Error,
                        Message = StringResources.NotFoundInDatabase,
                    });
                }

                string path = Server.MapPath(string.Concat(file.Path.Replace("ROOT", RootPath), '/', file.Name));

                if (file.IsFolder) {
                    if (!Directory.Exists(path)) {
                        return Json(new OperationResult {
                            Status = OperationStats.Error,
                            Message = StringResources.NotFoundInFileSystem,
                        });
                    }
                    // Remove it from File System
                    Directory.Delete(path, true);
                } else {
                    if (!System.IO.File.Exists(path)) {
                        return Json(new OperationResult {
                            Status = OperationStats.Error,
                            Message = StringResources.NotFoundInFileSystem,
                        });
                    }
                    // Remove it from File System
                    System.IO.File.Delete(path);
                }

                // Remove it's sub items and itself from Database
                Items = new List<FileItem>();
                await GetSubItemsAsync(await db.FileItems.Where(x => x.Id == file.Id).ToListAsync());
                Items.Reverse();
                foreach (var item in Items) {
                    db.FileItems.Remove(item);
                }

                db.FileItems.Remove(file);
                await db.SaveChangesAsync();

                return Json(new OperationResult {
                    Status = OperationStats.Success,
                    Message = StringResources.SuccessfullyDeleted
                });
            } catch (Exception ex) {
                throw;
            }
        }

        private async Task GetSubItemsAsync(List<FileItem> items) {
            foreach (FileItem item in items) {
                Items.Add(item);
                await GetSubItemsAsync(item.Files.ToList());
            }
        }

        private async Task UpdateSubDirectoryPath(List<FileItem> items) {
            foreach (var item in items) {
                item.Path = string.Concat(item.File.Path, '/', item.File.Name);
                await db.SaveChangesAsync();
                await UpdateSubDirectoryPath(item.Files.ToList());
            }
        }

        private string RenameFileOrDirectory(string path, string newName) {
            var dirName = string.Join("\\", path.Split('\\').Reverse().Skip(1).Reverse());

            return string.Concat(dirName, '\\', newName);
        }

        private List<ModelErrorCollection> GetErrors(ModelStateDictionary modelState) {
            return modelState.Select(x => x.Value.Errors)
                .Where(y => y.Count > 0)
                .ToList();
        }



        /// <summary>
        /// Create TreeView Here
        /// </summary>
        /// <returns></returns>
        public FileItemTreeNode GetTreeVeiwList()
        {
            FileItemTreeNode rootNode = db.FileItems
                                  .Where(a => a.FileId == null)
                                  .Select(x => new FileItemTreeNode
                                  {
                                      Id = x.Id,
                                      FileName = x.Name,
                                      FilePath = x.Path,
                                      isFolder = x.IsFolder
                                  })
                                  .SingleOrDefault();

            BuildChildNode(rootNode);

            return rootNode;
        }

        /// <summary>
        /// Generate TreeView Child Node
        /// </summary>
        /// <param name="rootNode">TreeView Parent Node</param>
        private void BuildChildNode(FileItemTreeNode rootNode)
        {
            if (rootNode != null)
            {
                List<FileItemTreeNode> childNode = db.FileItems.Where(a => a.FileId == rootNode.Id)
                                                              .Select(b => new FileItemTreeNode
                                                              {
                                                                  Id = b.Id,
                                                                  FileName = b.Name,
                                                                  FilePath = b.Path,
                                                                  isFolder = b.IsFolder
                                                              })
                                                              .OrderByDescending(c => c.isFolder)
                                                              .ToList<FileItemTreeNode>();

                if (childNode.Count > 0)
                {
                    foreach (var childRootNode in childNode)
                    {
                        BuildChildNode(childRootNode);
                        rootNode.ChildNode.Add(childRootNode);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}