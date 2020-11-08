using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace MemCheck.CommandLineDbClient.Geography
{
    static class ResourceFileReadHelper
    {
        public static ImmutableArray<string> GetFields(string line, int expectedCount)
        {
            IEnumerable<string> fields = line.Split(',');
            if (fields.Count() != expectedCount)
                throw new Exception($"Invalid line '{line}'");
            return fields.Select(field => field.Trim()).ToImmutableArray();
        }
    }

    sealed class Region
    //The examples are for Ile de France
    {
        //        REG	2	Code région
        //CHEFLIEU	5	Code de la commune chef-lieu
        //TNCC	1	Type de nom en clair
        //NCC	200	Nom en clair(majuscules)
        //NCCENR	200	Nom en clair(typographie riche)
        //LIBELLE	200	Nom en clair(typographie riche) avec article


        //11,75056,1,ILE DE FRANCE,Île-de-France,Île-de-France

    }
    sealed class Department
    //The examples are for Yvelines. The line is:
    //78,11,78646,4,YVELINES,Yvelines,Yvelines
    {
        #region Private methods
        private Department(string line)
        {
            var fields = ResourceFileReadHelper.GetFields(line, 7);
            DepartmentCode = fields[0];
            RegionCode = fields[1];
            CapitalCode = fields[2];
            NameType = fields[3];
            UppercasedName = fields[4];
            ShortName = fields[5];
            FullName = fields[6];
        }
        #endregion

        public string DepartmentCode { get; } //eg "78"
        private string RegionCode { get; }  //eg "11", which in the Region class is the code for Ile de France
        private string CapitalCode { get; } //eg "78646", which in the City class is the code for Versailles
        private string NameType { get; } //eg "4"
        private string UppercasedName { get; }   //eg "YVELINES"
        private string ShortName { get; }    //eg "Yvelines"
        public string FullName { get; } //eg "Yvelines"

        public static IEnumerable<Department> Read()
        {
            var contents = File.ReadAllLines(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\CSharp\Vinny\MemCheck_v0.2\MemCheck.CommandLineDbClient\RegionsFrancaises\Resources\departement2020.csv");
            return contents.Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => new Department(line));
        }

        //        DEP	3	Code département
        //REG	2	Code région
        //CHEFLIEU	5	Code de la commune chef-lieu
        //TNCC	1	Type de nom en clair
        //NCC	200	Nom en clair(majuscules)
        //NCCENR	200	Nom en clair(typographie riche)
        //LIBELLE	200	Nom en clair(typographie riche) avec article
    }
    sealed class City
    //The examples are for La Celle-Saint-Cloud. The line is:
    //COM,78126,11,78,784,3,CELLE SAINT CLOUD,Celle-Saint-Cloud,La Celle-Saint-Cloud,7804,
    {
        #region Private methods
        private City(string line)
        {
            var fields = ResourceFileReadHelper.GetFields(line, 11);
            CityType = fields[0];
            CityCode = fields[1];
            RegionCode = fields[2];
            DepartmentCode = fields[3];
            ArrondissementCode = fields[4];
            NameType = fields[5];
            UppercasedName = fields[6];
            ShortName = fields[7];
            FullName = fields[8];
            CantonCode = fields[9];
            ParentCityCode = fields[10];
        }
        #endregion
        private string CityType { get; } //eg "COM"
        private string CityCode { get; } //eg "78126", this is the INSEE code, not the postal code
        private string RegionCode { get; }   //eg "11", which in the Region class is the code for Ile de France
        private string DepartmentCode { get; }   //eg  "78"
        private string ArrondissementCode { get; }   //eg "784"
        private string NameType { get; } //eg "3"
        private string UppercasedName { get; }   //eg "CELLE SAINT CLOUD"
        private string ShortName { get; }    //eg "Celle-Saint-Cloud"
        private string FullName { get; } //eg "La Celle-Saint-Cloud"
        private string CantonCode { get; }   //eg "7804"
        private string ParentCityCode { get; }   //eg ""

        public static IEnumerable<City> Read()
        {
            var contents = File.ReadAllLines(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\CSharp\Vinny\MemCheck_v0.2\MemCheck.CommandLineDbClient\RegionsFrancaises\Resources\communes2020.csv");
            return contents.Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => new City(line));
        }
    }
    sealed class GeoInfo
    {
        public GeoInfo()
        {
            Cities = City.Read();
            DepartmentsFromName = Department.Read().ToImmutableDictionary(dep => dep.FullName, dep => dep);
        }
        public IEnumerable<City> Cities { get; }
        public ImmutableDictionary<string, Department> DepartmentsFromName;
    }
}
