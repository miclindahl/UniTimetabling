using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;
using UniversityTimetabling.StrategicOpt;


namespace UnitTests
{
    [TestFixture]
    internal class DataAnalysis : UnitTestBase
    {
        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void CalcKPIData(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");

            Console.WriteLine($"Dataset: {data.Name}");
            Console.WriteLine($"Courses: {data.Courses.Count} Lectures: {data.Courses.Sum(c => c.Lectures)}");
			Console.WriteLine($"Total timeslots: {data.TimeSlots.Count}");
			Console.WriteLine($"Total seats: {data.Rooms.Sum(r => r.Capacity)}");
            Console.WriteLine($"Average courses per curriculum: {data.Curricula.Average(q => q.Courses.Count) : 0.00} ");

            var includedRooms = data.Rooms;
            DisplayRoomStats("original",includedRooms.ToDictionary(r => r,r => 1), data);

            Console.WriteLine("\nLectures:\n");
            Console.WriteLine("size,hours");

            foreach (var coursesizes in data.Courses.GroupBy(c => c.NumberOfStudents).OrderBy(c => c.Key))
            {
                Console.WriteLine($"{coursesizes.Key},{coursesizes.Sum(s => s.Lectures)}");
            }
        }

        private static void DisplayRoomStats(string method,Dictionary<Room,int> includedRooms, Data data)
        {
            var utilizationprRoom = (double)data.Courses.Sum(c => c.Lectures) / (includedRooms.Sum(r => r.Value) * data.TimeSlots.Count);
            var utilizationPrSeat = (double)data.Courses.Sum(c => c.Lectures * c.NumberOfStudents) / (includedRooms.Sum(r => r.Value * r.Key.Capacity * data.TimeSlots.Count));


            Console.WriteLine($"Rooms: {includedRooms.Sum(kv => kv.Value)} Roomhours: {includedRooms.Count*data.TimeSlots.Count}");
            Console.WriteLine($"RoomsCost: {includedRooms.Sum(r => r.Key.Cost*r.Value)}");
            Console.WriteLine(
                $"Utilization pr. room: {utilizationprRoom : 0.00%} ");

            Console.WriteLine(
                $"Utilization pr. seat: {utilizationPrSeat : 0.00%} ");

            Console.WriteLine("\nRooms:\n");
            Console.WriteLine(String.Join(", ",includedRooms.Select(r => r.Key.ID + ": " + r.Value)));
            Console.WriteLine("\nsize,hours");
            foreach (var roomsize in includedRooms.GroupBy(r => r.Key.Capacity).OrderBy(r => r.Key))
            {
                Console.WriteLine($"{roomsize.Key},{roomsize.Sum(r => r.Value)*data.TimeSlots.Count}");
            }
            File.AppendAllText(@"c:\temp\roomutilization.txt", $"{method};{data.Filename};{utilizationprRoom : 0.00%};{utilizationPrSeat: 0.00%}\n");
        }

        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void MaximizeRoomUtilization(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");

            Console.WriteLine($"Dataset: {data.Name}");
            Console.WriteLine($"Courses: {data.Courses.Count} Lectures: {data.Courses.Sum(c => c.Lectures)}");

            
       
            //DisplayRoomStats("original",data.Rooms.ToDictionary(r => r, r => 1), data);

          //  var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);


            Console.WriteLine("\nLectures:\n");
            Console.WriteLine("size,hours");

            foreach (var coursesizes in data.Courses.GroupBy(c => c.NumberOfStudents).OrderBy(c => c.Key))
            {
                Console.WriteLine($"{coursesizes.Key},{coursesizes.Sum(s => s.Lectures)}");
            }

            var formulation = ProblemFormulation.MinimizeRoomCost;
            var par = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                UseHallsConditions =  true,
                UseRoomHallConditions = true,
                TuneGurobi = false,
                UseRoomsAsTypes = true,
            };
            var model = new CCTModel(data, formulation,par);
            model.Optimize();
            DisplayRoomStats("optimized",model.GetUsedRooms(), data);

        
        }

        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void MaximizeRoomUtilizationSimplemodel(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");

            Console.WriteLine($"Dataset: {data.Name}");
            Console.WriteLine($"Courses: {data.Courses.Count} Lectures: {data.Courses.Sum(c => c.Lectures)}");
            var formulation = ProblemFormulation.MinimizeRoomCost;

            var newrooms = CreateRooms(data);
            data.SetRoomList(newrooms);
            var model = new RoomChoose(data, formulation);
            model.Optimize();
            var usedrooms = model.GetUsedRooms();
            DisplayRoomStats("optimizedLowerBound",usedrooms, data);

            var roomcalc = new RoomCombinations(data);
            Assert.LessOrEqual(Math.Abs(roomcalc._minCost - usedrooms.Sum(r => r.Key.Cost*r.Value)), 1e-1,"something wrong");
        }

        [TestCase(ITC_Comp18)]
        public void HeurusticRoomChoose(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");

            Console.WriteLine($"Dataset: {data.Name}");
            Console.WriteLine($"Courses: {data.Courses.Count} Lectures: {data.Courses.Sum(c => c.Lectures)}");
            var formulation = ProblemFormulation.MinimizeRoomCost;

            var newrooms = CreateRoomsFixedSize(data,25,1);
            data.SetRoomList(newrooms);
            var solutions = new List<Dictionary<Room,int>>();
            for (var overcapacity = 0; overcapacity < 100; overcapacity+=10)
            {
                var model = new RoomChoose(data, formulation,overcapacity);
                model.Optimize();
                
                var usedrooms = model.GetUsedRooms();
                solutions.Add(usedrooms);
                DisplayRoomStats("testest", usedrooms, data);
            }

            var nsizes = newrooms.Select(r => r.Capacity).Distinct().ToList();
            nsizes.Add(0);
            nsizes.Sort();
           // nsizes.RemoveAt(nsizes.Count-1);

            Console.WriteLine("\nAccumulated Timeslots Free / #lecture (>n):");
            Console.WriteLine($"Cost;{string.Join(";", nsizes.Select(r => "n" + r))}");

            foreach (var sol in solutions)
            {
                var availRoomsHours = nsizes.ToDictionary(n => n, n => sol.Where(rkv => rkv.Key.Capacity > n).Sum(rt => rt.Value * data.TimeSlots.Count));
                var freeTimeslots = nsizes.ToDictionary(n => n,
                    n => availRoomsHours[n] - data.Courses.Where(a => a.NumberOfStudents > n).Sum(c => c.Lectures));
                Console.WriteLine(
                    $"{sol.Sum(s => s.Value*s.Key.Cost)};{string.Join(";", nsizes.Select(r => $"{((double) availRoomsHours[r] / data.Courses.Where(a => a.NumberOfStudents > r).Sum(c => c.Lectures)) -1:0.00}")) }");
            }



        }

        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void MinimizeTimeslotSimplemodel(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");

            Console.WriteLine($"Dataset: {data.Name}");
            Console.WriteLine($"Courses: {data.Courses.Count} Lectures: {data.Courses.Sum(c => c.Lectures)}");
            Console.WriteLine($"Rooms: {data.Rooms.Count}");
            Console.WriteLine($"LB timeslot: {data.Courses.Sum(c => c.Lectures) / data.Rooms.Count}");
            

            var formulation = ProblemFormulation.MinimizeRoomCost;
            var model = new TimeSlotChoose(data, formulation);
            model.Optimize();
            

        }



        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void TimeSlotAnalysis(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            Console.WriteLine($"Dataset: {data.Name}");

            Console.WriteLine($"Days: {data.Days} \nPerDay: {data.TimeSlotsByDay[0].Count} \nTimeslots: {data.TimeSlots.Count}");

           
            foreach (var timeSlot in data.TimeSlots)
            {
                Console.WriteLine($"Timeslot: {timeSlot}  unavailblecourses: {(double) data.Courses.Count(c => c.UnavailableTimeSlots.Contains(timeSlot)) / data.Courses.Count : 0.00%}");
            }

            Console.WriteLine("\nCourses:");
            foreach (var course in data.Courses)
            {
                Console.WriteLine($"Course: {course}  Unavailble: {(double) course.UnavailableTimeSlots.Count/data.TimeSlots.Count : 0.00%}");
            }



            Console.WriteLine();
            //data.TimeSlots.Add(new TimeSlot(0,1));
            //data.Courses.First().un
            //data.RecalculateTimeSlotsByDay = true;
        }


        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp21)]
        public void TopSolutionsAnalysis(string filename)
        {
            var name = Path.GetFileNameWithoutExtension(filename);

            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var courestime = new Dictionary<Course,Dictionary<TimeSlot,int>>();
            var penaltyforcourse = new Dictionary<Course,int>();
            foreach (var course in data.Courses)
            {
                courestime.Add(course,new Dictionary<TimeSlot, int>());
                foreach (var timeSlot in data.TimeSlots)
                {
                    courestime[course].Add(timeSlot,0);
                }
                penaltyforcourse[course] = 0;
            }


            var bestsol = new List<Assignment>();
            var penMWD = new List<int>();
            var penCC = new List<int>();
            for (var i = 0; i < 10; i++)
            {
                var sol = new Solution(data, formulation);
                sol.Read(dataPath + @"ITC2007\sol\" + name + "-UD2-" + i + ".sol");
                sol.AnalyzeSolution();
                foreach (var assignment in sol._assignments)
                {
                    courestime[assignment.Course][assignment.TimeSlot]++;
                }

                foreach (var course in data.Courses)
                {
                    penaltyforcourse[course] += sol.PenaltyForCourse[course];
                }
                penMWD.Add((int) sol.MinimumWorkingDays);
                penCC.Add((int) sol.CurriculumCompactness);

                if (i == 0) bestsol = sol._assignments.ToList();
            }

            const int minhint = 7;
            var maxhint = 7;
            var noise = 0.5;

            const bool fixhints = true;

            var random = new Random(62);

            foreach (var course in data.Courses)
            {
                foreach (var ts in data.TimeSlots)
                {
                  //     if (random.Next(10) < courestime[course][ts]) courestime[course][ts] = 1;
                  //    else courestime[course][ts] = 0;

                       if (courestime[course][ts] < minhint) courestime[course][ts] = 0;
                       if (courestime[course][ts] > maxhint) courestime[course][ts] = 0;
                       //result should be made to double.
                      // if(courestime[course][ts] > 0) courestime[course][ts] +=  random.Next(-3,3);
                    //courestime[course][ts] *= 1 + (int) Math.Round(noise*(2*random.NextDouble()-1));
                }
            }
            
            var hints = courestime.SelectMany(c => c.Value.Where(cv => cv.Value > 0).Select(cv => Tuple.Create(c.Key, cv.Key, cv.Value))).ToList();
            var currtime = new Dictionary<Curriculum, Dictionary<TimeSlot, int>>();
            foreach (var curr in data.Curricula)
            {
                currtime.Add(curr, new Dictionary<TimeSlot, int>());
                foreach (var timeSlot in data.TimeSlots)
                {
                    currtime[curr].Add(timeSlot,courestime.Where(c => curr.Courses.Contains(c.Key)).Sum(kv => kv.Value[timeSlot]));
                }
             }
            var chints = currtime.SelectMany(c => c.Value.Select(cv => Tuple.Create(c.Key, cv.Key, cv.Value))).ToList();


            Console.WriteLine($"Timeslots: {data.TimeSlots.Count}");
            Console.WriteLine($"MWD: avg: {penMWD.Average()} - {string.Join(", ", penMWD)} ");
            Console.WriteLine($"CC: avg: {penCC.Average()} - {string.Join(", ", penCC)} ");
            foreach (var course in data.Courses.OrderByDescending(c => penaltyforcourse[c]))
            {
                Console.WriteLine($"\nCourse: {course}\n" +
                                  $"Size: {course.NumberOfStudents}\n" +
                                  $"Lectures: {course.Lectures}\n" +
                                  $"MWD: {course.MinimumWorkingDays}\n" +
                                  $"TSAvaialble:{data.TimeSlots.Count - course.UnavailableTimeSlots.Count}\n" +
                                  $"Unavail: {string.Join(",", course.UnavailableTimeSlots)}\n" +
                                  $"TotalPenalty: {penaltyforcourse[course]}");
                foreach (var valuePair in courestime[course].Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value))
                {
                    Console.WriteLine(valuePair.Key + ": " + valuePair.Value);
                }
            }

            Console.WriteLine("\n\nBy prio: ");
            foreach (var hint in hints.Where(h => h.Item3 > 0).OrderByDescending(h => h.Item3))
            {
                Console.WriteLine(hint.Item1.ID + "_" + hint.Item2 + ": " + hint.Item3);
            }

            Console.WriteLine("\n\nHistogram: ");
            Console.WriteLine("total lectures: " + data.Courses.Sum(c => c.Lectures));
            for (var i = 1; i <= 10; i++)
            {
                Console.WriteLine($"{i}\t{hints.Count(h => h.Item3 == i)}");

            }

            Console.WriteLine("Unavailabilites\tSame");
            foreach (var course in data.Courses)
            {
                Console.WriteLine($"{course.UnavailableTimeSlots.Count}\t{(double) courestime[course].Sum(t => t.Value)/course.Lectures : 0.00}");
            }
            Console.WriteLine("Penal\tSame");
            foreach (var course in data.Courses)
            {
                Console.WriteLine($"{penaltyforcourse[course]}\t{(double) courestime[course].Sum(t => t.Value)/course.Lectures : 0.00}");
            }
            Console.WriteLine("Size\tSame");
            foreach (var course in data.Courses)
            {
                Console.WriteLine($"{course.NumberOfStudents}\t{(double) courestime[course].Sum(t => t.Value)/course.Lectures : 0.00}");
            }
           // return;
            //Console.WriteLine("\n\nCurriculumPatterns");

            //foreach (var hint in chints.Where(h => h.Item3 > 0).OrderByDescending(h => h.Item3))
            //{
            //    Console.WriteLine(hint.Item1.ID + "_" + hint.Item2 + ": " + hint.Item3);
            //}
            //Console.WriteLine("\n\nHistogram: ");
            //Console.WriteLine("total curr_lectures: " + data.Curricula.Sum(c => c.Lectures));
            //for (var i = 1; i <= 10; i++)
            //{
            //    Console.WriteLine($"{i}\t{chints.Count(h => h.Item3 == i)}");

            //}


            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
              //  Seed = seeds[i],
            };
            
                var model = new CCTModel(data, formulation, ModelParameters);
                // model.SetMipHints(chints);
                Console.WriteLine($"Adding {hints.Count} hints. Total letures: {data.Courses.Sum(c => c.Lectures)} ({(double) hints.Count / data.Courses.Sum(c => c.Lectures) :0.00%})");
            model.SetMipHints(hints,fixhints);
                // model.MipStart(bestsol);
                var r = model.Optimize(300);
            // model.Optimize(60*60*3);

            var ass = model.GetAssignments();
            var finalsol = new Solution(data, formulation);
            finalsol.SetAssignments(ass);
            finalsol.Save(@"C:\temp\finalsolStageI_" + name + ".sol");
            var ModelParameters2 = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = true,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                //  Seed = seeds[i],
            };

            var model2 = new CCTModel(data, formulation, ModelParameters2);
            model2.SetAndFixSolution(ass);
            model2.Optimize(300);
            ass = model2.GetAssignments();
            finalsol.SetAssignments(ass);
            finalsol.Save(@"C:\temp\finalsolStageII_" + name + ".sol");


        }




    }
}
