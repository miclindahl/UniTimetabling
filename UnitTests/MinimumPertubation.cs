using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;

namespace UnitTests
{

    [TestFixture]
    class MinimumPertubation : UnitTestBase
    {
        private bool ConsoleToFile = false;

     //   private ProblemFormulation formulation = ProblemFormulation.UD2NoOverbook;
        private ProblemFormulation formulation = ProblemFormulation.UD2;

        [Test, Combinatorial]
        //[TestCaseSource(nameof(TestDatasetsITC2007))]
        public void QualityRecovering([Values(
            QualityRecoveringOptimizer.DisruptionTypes.CurriculumFourCoursesInsert,
         //   QualityRecoveringOptimizer.DisruptionTypes.CurriculumFourCoursesInsertWithCC,
                QualityRecoveringOptimizer.DisruptionTypes.RandomRoomRandomdayRemoved,
                QualityRecoveringOptimizer.DisruptionTypes.OneTimeslotUnavailable,
                QualityRecoveringOptimizer.DisruptionTypes.OneAssignmentUnavailbility
            )] QualityRecoveringOptimizer.DisruptionTypes disruption, [Values(
            //ITC_Comp01,ITC_Comp05,ITC_Comp10,ITC_Comp12,ITC_Comp14,ITC_Comp18
            
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
                ITC_Comp21
                )] string filename, [Values(1)] int seed)
        {
            var disruptionname = disruption;

			// if (ConsoleToFile)
			//     SetConsoleOutputToFile(@"c:\temp\minimumpertubation\\logs\\" + filename + "\\" + disruptionname + ".log");
			seed = GetRealSeed(filename, seed);
			var data = Data.ReadXml(dataPath + filename, "120", "4");

            var name = Path.GetFileNameWithoutExtension(filename);
            var modelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = true,
            };

            var solutionBefore = new Solution(data, formulation);
            solutionBefore.Read(dataPath + @"ITC2007\sol\" + name + "-UD2" + ".sol");
            Console.WriteLine(solutionBefore.AnalyzeSolution());

            var minimumPertubation = new QualityRecoveringOptimizer(data, solutionBefore, formulation, modelParameters)
            {
                Timelimit = 60*60*10,
                ExtraPerubations =  100,
                MaxTotalPertubations = 20,
                SolPerPertubation = 1,
                PerturbationObjectEqual = true,
			};
	        string entitiesdisrupted;
			var affected = minimumPertubation.Disrupt(disruption, seed, out entitiesdisrupted);


            RunPertubationTest(seed, minimumPertubation, solutionBefore, disruptionname.ToString(), name, affected, entitiesdisrupted);
        }

		/// <summary>
		/// Creates a seed dependent on the filename.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="seed"></param>
		/// <returns></returns>
	    private int GetRealSeed(string filename, int seed)
	    {
		    int seed2;
		    unchecked
		    {
			    seed2 = filename.GetHashCode() ^ seed;
		    }
		    return seed2;
	    }

	    [Test, Combinatorial]
        //[TestCaseSource(nameof(TestDatasetsITC2007))]
        public void MinimumPertubationsAssignments([Values(
            ITC_Comp01,ITC_Comp05,ITC_Comp10,ITC_Comp12,ITC_Comp14,ITC_Comp18
            /*
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
                ITC_Comp21*/
                )] string filename, [Values(1)] int seed)
        {

            var data = Data.ReadXml(dataPath + filename, "120", "4");

            var name = Path.GetFileNameWithoutExtension(filename);
            var modelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = true,
            };

            var solutionBefore = new Solution(data, formulation);
            solutionBefore.Read(dataPath + @"ITC2007\sol\" + name + "-UD2" + ".sol");
            solutionBefore.AnalyzeSolution();

            for (var n = 0; n < 100; n++)
            {
                if (n > solutionBefore._assignments.Count) break;

                var disruptionname = nameof(MinimumPertubationsAssignments) + n;

                
                var minimumPertubation = new QualityRecoveringOptimizer(data, solutionBefore, formulation, modelParameters)
                {
                    Timelimit = 60*60,
                    ExtraPerubations = 0,
                    SolPerPertubation = 1
                };


                var affected = minimumPertubation.DisruptAssignmentUnavailable(seed, n);
                
                RunPertubationTest(seed, minimumPertubation, solutionBefore, disruptionname, name, affected,"");
            }
        }




        private class AssignmentStat
        {
            public int Nused = 0;
            public int Minperb = Int32.MaxValue;
            public int Maxperb = int.MinValue;
            public int Minobj = Int32.MaxValue;
            public int Maxobj = int.MinValue;
        }


        private static void RunPertubationTest(int seed, QualityRecoveringOptimizer minimumPertubation, Solution solutionBefore,
            string disruptionname, string name, int affected,string entitiesdisrupted)
        {
            var solutions = minimumPertubation.Run();


            Console.WriteLine("Solutions:");
            for (var i = 0; i < solutions.Count; i++)
            {
                Console.WriteLine(new string('-', 15));
                Console.WriteLine($"Pertubation: {solutions[i].Item1}\n" +
                                  $"Objective: {(int) solutions[i].Item2.Objective}");
                var assignments = solutions[i].Item2._assignments.ToList();
                minimumPertubation.DisplayPertubations(solutionBefore._assignments.ToList(),
                    assignments);

                if (i > 0)
                {
                    var changesbefore = solutions[i-1].Item2._assignments.ToList().Except(solutionBefore._assignments);
                    var changes= assignments.Except(solutionBefore._assignments).ToList();
                    var newfrombefore = changes.Except(changesbefore).ToList();
                    Console.WriteLine($"New assignment from before: {newfrombefore.Count}/{solutions[i].Item1}\n" +
                    $"{string.Join(",", newfrombefore)}\n");
                }
            }
            // inSols , MinPerb, MaxPerb, Minobj, Maxobj
            
            var assignmentsStats = new Dictionary<Assignment,AssignmentStat>();
            foreach (var solution in solutions)
            {
                var newassignments = solution.Item2._assignments.Except(solutionBefore._assignments);
                foreach (var assignment in newassignments)
                {
                    if (!assignmentsStats.ContainsKey(assignment))
                        assignmentsStats[assignment] = new AssignmentStat();
                    assignmentsStats[assignment].Nused++;
                    assignmentsStats[assignment].Minperb = Math.Min(assignmentsStats[assignment].Minperb,solution.Item1);
                    assignmentsStats[assignment].Maxperb = Math.Max(assignmentsStats[assignment].Minperb,solution.Item1);
                    assignmentsStats[assignment].Minobj = Math.Min(assignmentsStats[assignment].Minobj,(int) solution.Item2.Objective);
                    assignmentsStats[assignment].Maxobj = Math.Max(assignmentsStats[assignment].Maxobj,(int) solution.Item2.Objective);
                }
            }
            Console.WriteLine("Assignments:\n" +
                              "Assignment;nused;minperb;maxperb;minobj;maxobj");
            foreach (var assignmentStat in assignmentsStats)
            {
                Console.WriteLine($"{assignmentStat.Key};{assignmentStat.Value.Nused};{assignmentStat.Value.Minperb};{assignmentStat.Value.Maxperb};{assignmentStat.Value.Minobj};{assignmentStat.Value.Maxobj}");
            }


            Console.WriteLine($"Before Disruption:\n" +
                              $"{solutionBefore.AnalyzeSolution()}" +
                              $"Potential Pertu " +
                              $"bations:\n" +
                              $"Perb,obj,MWD,CC,RC,RS,s\n" +
                              $"{string.Join("\n", solutions.Select(s => $"{s.Item1},{(int) s.Item2.Objective},{(int) s.Item2.MinimumWorkingDays},{(int) s.Item2.CurriculumCompactness},{(int) s.Item2.RoomCapacity},{(int) s.Item2.RoomStability},{s.Item3}"))}");

            var sumfile = @"c:\temp\minimumpertubation\summary.csv";
            if (!File.Exists(sumfile))
                File.AppendAllText(sumfile,
                    $"date;disruptiontype;seed;dataset;disruptions;disrupted;minimumperb;objbefore;minobj;maxobj;maxperb;minperbruntime");

            var time = DateTime.Now;

            if (solutions.Count == 0)
            {
                File.AppendAllText(sumfile,
                    $"\n{time};{disruptionname};{seed};{name};{affected};{entitiesdisrupted};;{solutionBefore.Objective};;;;");
                return;
            }
            File.AppendAllText(sumfile,
                $"\n{time};{disruptionname};{seed};{name};{affected};{entitiesdisrupted};{solutions.Min(t => t.Item1)};{solutionBefore.Objective};" +
                $"{solutions.Min(t => t.Item2.Objective)};{solutions.Max(t => t.Item2.Objective)};" +
                $"{solutions.Max(t => t.Item1)};{solutions.OrderBy(s => s.Item1).First().Item3}");

            var allfile = @"c:\temp\minimumpertubation\summaryall.csv";
            if (!File.Exists(allfile))
                File.AppendAllText(allfile,
                    $"date;disruptiontype;seed;dataset;solbefore;affected;disrupted;pertubations;solution;runtime;pertubationsScaled;solutionScaled");

            foreach (var solution in solutions)
            {
                File.AppendAllText(allfile,
                    $"\n{time};{disruptionname};{seed};{name};{(int) solutionBefore.Objective};{affected};{entitiesdisrupted};{solution.Item1};{(int) solution.Item2.Objective};{solution.Item3};" +
                    $"{(Math.Abs(solutions.Max(t => t.Item1) - solutions.Min(t => t.Item1)) < 0.5 ? 1.00 : (double) (solution.Item1 - solutions.Min(t => t.Item1))/(solutions.Max(t => t.Item1) - solutions.Min(t => t.Item1))) : 0.0000};" +
                    $"{(Math.Abs(solutions.Max(t => t.Item2.Objective) - solutions.Min(t => t.Item2.Objective)) < 0.5 ? 0.0 : (double)(solution.Item2.Objective - solutions.Min(t => t.Item2.Objective)) / (solutions.Max(t => t.Item2.Objective) - solutions.Min(t => t.Item2.Objective))) : 0.0000}");
            }
        }
    }
}
