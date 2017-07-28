using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;
using UniversityTimetabling.StrategicOpt;

namespace UnitTests
{
    public class UnitTestBase
    {
      

        public static string dataPath = @"..\..\Resources\CCT\";
        public const string ITC_Comp01 = @"ITC2007\comp01.xml";
        public const string ITC_Comp02 = @"ITC2007\comp02.xml";
        public const string ITC_Comp03 = @"ITC2007\comp03.xml";
        public const string ITC_Comp04 = @"ITC2007\comp04.xml";
        public const string ITC_Comp05 = @"ITC2007\comp05.xml";
        public const string ITC_Comp06 = @"ITC2007\comp06.xml";
        public const string ITC_Comp07 = @"ITC2007\comp07.xml";
        public const string ITC_Comp08 = @"ITC2007\comp08.xml";
        public const string ITC_Comp09 = @"ITC2007\comp09.xml";
        public const string ITC_Comp10 = @"ITC2007\comp10.xml";
        public const string ITC_Comp11 = @"ITC2007\comp11.xml";
        public const string ITC_Comp12 = @"ITC2007\comp12.xml";
        public const string ITC_Comp13 = @"ITC2007\comp13.xml";
        public const string ITC_Comp14 = @"ITC2007\comp14.xml";
        public const string ITC_Comp15 = @"ITC2007\comp15.xml";
        public const string ITC_Comp16 = @"ITC2007\comp16.xml";
        public const string ITC_Comp17 = @"ITC2007\comp17.xml";
        public const string ITC_Comp18 = @"ITC2007\comp18.xml";
        public const string ITC_Comp19 = @"ITC2007\comp19.xml";
        public const string ITC_Comp20 = @"ITC2007\comp20.xml";
        public const string ITC_Comp21 = @"ITC2007\comp21.xml";

        public readonly List<string> TestDatasetsITC2007 = new List<string>() {
         ITC_Comp01,
         ITC_Comp02,
         ITC_Comp03,
         ITC_Comp04,
         ITC_Comp05,
         ITC_Comp06,
         ITC_Comp07,
         ITC_Comp08,
         ITC_Comp09,
         ITC_Comp10,
         ITC_Comp11,
         ITC_Comp12,
         ITC_Comp13,
         ITC_Comp14,
         ITC_Comp15,
         ITC_Comp16,
         ITC_Comp17,
         ITC_Comp18,
         ITC_Comp19,
         ITC_Comp20,
         ITC_Comp21,
        };

        public readonly List<string> TestMultiObjTestBed = new List<string>() {
         ITC_Comp01,
         ITC_Comp05,
         ITC_Comp06,
         ITC_Comp08,
         ITC_Comp12,
         ITC_Comp18,
        };



        public const string Udine1 = @"Udine\Udine1.xml";
        public const string Udine2 = @"Udine\Udine2.xml";
        public const string Udine3 = @"Udine\Udine3.xml";

        public const string Erlangen2012_1 = @"Erlangen\erlangen2012_1.xml";
        public const string Erlangen2012_2 = @"Erlangen\erlangen2012_2.xml";
        public const string Erlangen2014_1 = @"Erlangen\erlangen2014_1.xml";
        private readonly List<string> TestDatasetsErlangen = new List<string>() {
            Erlangen2012_1,
            Erlangen2012_2,
            Erlangen2014_1
        };

        [SetUp]
        public void Init()
        {
            Console.WriteLine(DateTime.Now);
         
        }

        private TextWriter _oldConsoleOut;
        private FileStream _writeFileStream;
        private StreamWriter _streamWriter;

        public void SetConsoleOutputToFile(string filename)
        {
            var dir = new FileInfo(filename).Directory;
            if (!dir.Exists) dir.Create();

            _writeFileStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
            _streamWriter = new StreamWriter(_writeFileStream);
            _oldConsoleOut = Console.Out;
            _streamWriter.AutoFlush = true;
            Console.SetOut(_streamWriter);
            Console.WriteLine(DateTime.Now);
        }

        public void DisplayParameterObject(object obj)
        {

            Console.WriteLine("\n" + obj.GetType().Name);
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine("\t{0}={1}", prop.Name, prop.GetValue(obj, null));
            }
        }

        public void WriteSol(string resultdir,string dataset,Data data, ProblemFormulation prolemFormulation, string algorithm,string datetime, List<MultiObjectiveSolver.MultiResult> result)
        {
            var dataname = System.IO.Path.GetFileNameWithoutExtension(dataset);
            var resultfile = resultdir + "results.csv";
            if (!File.Exists(resultfile))
                File.AppendAllText(resultfile, $"dataset;algorithm;datetime;i;cost;SoftObj;CostBound;SoftObjBound;CostConst;QualConst;Seconds");

            int i = 0;
            foreach (var res in result)
            {
                i++;
                File.AppendAllText(resultfile, $"\n{dataname};{algorithm};{datetime};{i};{res.Cost:##.#};{res.SoftObjective};{res.CostBound ?? double.NaN};{res.SoftObjBound ?? double.NaN};{res.CostConstraint ?? double.NaN};{res.QualConstraint ?? double.NaN};{res.SecondsSinceStart ?? double.NaN}");

                if (res.Assignments != null)
                {
                    var solution = new Solution(data,prolemFormulation);
                    solution.SetAssignments(res.Assignments);
                    solution.Save($"{resultdir}solutions\\{dataname}_{algorithm}_{i}.sol");
                }
            }
        }


        public void WriteSol(string resultdir,string dataset,Data data, ProblemFormulation prolemFormulation, string algorithm,string datetime, List<Tuple<int,int,int,int>> result)
        {
            var dataname = Path.GetFileNameWithoutExtension(dataset);
            var resultfile = resultdir + "resultsTimeslot.csv";
            if (!File.Exists(resultfile))
                File.AppendAllText(resultfile, $"dataset;algorithm;datetime;i;timeslots;obj;LB;s");

            int i = 0;
            foreach (var res in result)
            {
                i++;
                File.AppendAllText(resultfile, $"\n{dataname};{algorithm};{datetime};{i};{res.Item1:##.#};{res.Item2};{res.Item3};{res.Item4}");
            }
        }



        public static List<Room> CreateRooms(Data data, int nRoomOfEachSize = 1)
        {
            var sizes = data.Courses.GroupBy(c => c.NumberOfStudents);
            //todo use course sizes to create apropiate rooms
            var newrooms = new List<Room>();
            foreach (var size in sizes.OrderBy(s => s.Key))
            {
                var i = size.Key;
                var roomsofsize =(int) Math.Ceiling((double) size.Sum(c => c.Lectures) / data.TimeSlots.Count);
               // Console.WriteLine(roomsofsize);
                for (var j = 0; j < nRoomOfEachSize; j++)
                {
                    var roomLetter = Convert.ToChar('A' + j);
                    newrooms.Add(new Room($"Room{roomLetter}{i}", i));
                }
            }
            return newrooms;
        }
        public static List<Room> CreateRoomsFixedSize(Data data, int step,int nRoomOfEachSize)
        {
            var sizes = data.Courses.GroupBy(c => c.NumberOfStudents);
            //todo use course sizes to create apropiate rooms
            var maxroomsize = Math.Ceiling((double)data.Courses.Max(c => c.NumberOfStudents) / step) * step;
            var newrooms = new List<Room>();
            for (var i = step; i <= maxroomsize; i+= step)
            { 
              //  var i = size.Key;
             //   var roomsofsize =(int) Math.Ceiling((double) size.Sum(c => c.Lectures) / data.TimeSlots.Count);
                for (var j = 0; j < nRoomOfEachSize; j++)
                {
                    var roomLetter = Convert.ToChar('A' + j);
                    newrooms.Add(new Room($"Room{roomLetter}{i}", i));
                }

            }
            return newrooms;
        }


        public static List<List<Room>> CreateRoomsFromIntList(Data data, List<Dictionary<int, int>> combinations)
        {
            var allRooms = new List<List<Room>>();
            foreach (var combination in combinations)
            {
                var newrooms = new List<Room>();
                foreach (var c in combination)
                {
                    for (var i = 0; i < c.Value; i++)
                    {

                        var roomLetter = Convert.ToChar('A' + i);
                        newrooms.Add(new Room($"Room{roomLetter}{c.Key}", c.Key));
                    }
                }
                allRooms.Add(newrooms);
            }
            return allRooms;
        }


        public static void CreateExtraTimeslotEachDay(Data data,int numberperday)
        {
            var random = new Random(60);

            var initialpercentage = new Dictionary<Course,double>();
            foreach (var course in data.Courses)
            {
                  initialpercentage[course] = (double) course.UnavailableTimeSlots.Count/data.TimeSlots.Count;
            }

            for (int j = 0; j < numberperday; j++)
            {
                for (int i = 0; i < data.Days; i++)
                {
                    var next = data.TimeSlotsByDay[i].Count + 1;
                    var newtimeslot = new TimeSlot(i, next);
                    data.TimeSlots.Add(newtimeslot);
                    foreach (var course in data.Courses)
                    {
                        if (random.NextDouble() < initialpercentage[course]) course.UnavailableTimeSlots.Add(newtimeslot);
                    }
                }
                data.Periods++;
                data.RecalculateTimeSlotsByDay = true;

            }
        }

    }
}
