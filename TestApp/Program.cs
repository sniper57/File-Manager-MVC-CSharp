using CloudSdk.DirectoryServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static public void Main(string[] args)
        {
            Console.WriteLine("Enter Name:"); //try willy
            var getname = Console.ReadLine();
            var output = GetEmployeByName(getname);
            if (output != null)
            {
                foreach (var item in output)
                {
                    Console.WriteLine(item.AccountName + "\t" + item.Department + "\t" + item.DisplayName + "\t\t" + item.Email + "\t\t" + item.Supervisor + "\t\t" + item.Title);
                }
                Console.WriteLine("Press anykey to continue");
                Console.ReadLine();
            }
            

        }






        public class EmployeeDetailsViewModel
        {
            public string DisplayName { get; set; }
            public string Title { get; set; }
            public string Department { get; set; }
            public string Email { get; set; }
            public string AccountName { get; set; }
            public string Supervisor { get; set; }
        }

        public static List<EmployeeDetailsViewModel> GetEmployeByName(string Name)
        {
            List<DirectoryUser> Temp = new List<DirectoryUser>();
            CloudSdkDirectory AD = new CloudSdkDirectory();
            EmployeeDetailsViewModel TempData = new EmployeeDetailsViewModel();
            List<EmployeeDetailsViewModel> ReturnList = new List<EmployeeDetailsViewModel>();

            if (Name == null)
            {
                return null;
            }


            if (Name.Contains(",") == true)
            {
                if (AD.GetUsersByName(Name.Split(',')[0].ToString().Trim(), Name.Split(',')[1].ToString().Trim(), "") != null)
                {
                    Temp = AD.GetUsersByName(Name.Split(',')[0].ToString().Trim(), Name.Split(',')[1].ToString().Trim(), "");
                }

                if (AD.GetUsersByName(Name.Split(',')[1].ToString().Trim(), Name.Split(',')[0].ToString().Trim(), "") != null)
                {
                    Temp.AddRange(AD.GetUsersByName(Name.Split(',')[1].ToString().Trim(), Name.Split(',')[0].ToString().Trim(), ""));
                }
            }
            else if (Name.Trim().Contains(" ") == true)
            {
                if (AD.GetUsersByName(Name.Split(' ')[0].ToString().Trim(), Name.Split(' ')[1].ToString().Trim(), "") != null)
                {
                    Temp = AD.GetUsersByName(Name.Split(' ')[0].ToString().Trim(), Name.Split(' ')[1].ToString().Trim(), "");
                }

                if (AD.GetUsersByName(Name.Split(' ')[1].ToString().Trim(), Name.Split(' ')[0].ToString().Trim(), "") != null)
                {
                    Temp.AddRange(AD.GetUsersByName(Name.Split(' ')[1].ToString().Trim(), Name.Split(' ')[0].ToString().Trim(), ""));
                }
            }
            else if (Name.Trim().Contains("ms/") || Name.Trim().Contains("ms\\"))
            {
                Temp.Add(AD.GetUserByUserId(Name.Replace("ms/", "").Replace("ms\\", "")));
            }
            else if (Name.Trim() == "")
            {
                //do nothing
            }
            else
            {
                if (AD.GetUsersByName(Name.Trim()) != null)
                {
                    Temp = AD.GetUsersByName(Name.Trim());
                }

                if (AD.GetUsersByName("", Name.Trim(), "") != null)
                {
                    Temp.AddRange(AD.GetUsersByName("", Name.Trim(), ""));
                }

                //For MSID Search | Patrick
                if (AD.GetUserByUserId(Name.Trim()) != null)
                {
                    Temp.Add(AD.GetUserByUserId(Name.Trim()));
                }

            }


            foreach (var item in Temp.Take(200).ToList())
            {
                TempData = new EmployeeDetailsViewModel();
                TempData.DisplayName = item.DisplayName;
                TempData.Title = "";
                TempData.Department = item.Department;
                TempData.Email = item.Email;
                TempData.AccountName = item.DomainUserId;
                TempData.Supervisor = (item.DomainName != null ? item.Manager.DisplayName : ""); //Modified by Willy David - 02.01.2018 

                if (ReturnList.Contains(TempData) == false && TempData.AccountName != "" && TempData.Email != "")
                {
                    ReturnList.Add(TempData);
                }
            }

            return ReturnList;

        }

    }
}
