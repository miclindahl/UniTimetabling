using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;
using UniversityTimetabling.StrategicOpt;

namespace UnitTests
{

    [TestFixture]
    internal class MultiObjectiveUnitTests : UnitTestBase
    {
        private int Timelimit = 60*15;//*60;

        private bool ConsoleToFile = false;
        private ProblemFormulation _problemFormulation = ProblemFormulation.UD2NoOverbook;// ProblemFormulation.UD2NoNoOverbookNoUnAvilability;
        

        [Test,Combinatorial]
     //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void MultiCornerPointsTest([Values(
       /*  ITC_Comp01,
         ITC_Comp05,
         ITC_Comp06,
         ITC_Comp08,
         ITC_Comp12,
            ITC_Comp11,
         ITC_Comp18 
         */
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
            )] string filename,
            [Values(true)] bool EpsilonOnQuality,
            [Values(true)] bool SpecificQual)
        {
            var algoname = nameof(MultiCornerPointsTest) + $"EpsOnQual_{EpsilonOnQuality}_SpecQual_{SpecificQual}_4timeinstart";
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
           // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                // UseStageIandII = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                ConstraintPenalty = 50,
                UseRoomsAsTypes = true,
            };
            var solver = new MultiObjectiveSolver(data,_problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                Steps = 0,
                EpsilonOnQuality = EpsilonOnQuality,
               UseSpecificObjConstraints = false,
               ExtraTimeOnCornerPointsMultiplier = 2,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);
        }

        [Test, Combinatorial]
        //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void WeightedSumTest([Values(
            ITC_Comp05,
         ITC_Comp18)] string filename)
        {
            var algoname = nameof(WeightedSumTest);
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
            // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                // UseStageIandII = true,
                UseRoomsAsTypes = true,
            };
            //var problemFormulation = ProblemFormulation.EverythingZero;
            //problemFormulation.BadTimeslotsWeight = 1;
            var solver = new MultiObjectiveSolver(data, _problemFormulation, mipModelParameters)
            {
                Timelimit = 120,//Timelimit,
                ExtraTimeOnCornerPointsMultiplier = 1,
                UseEpsilonMethod = false,
                Steps = 3
            };

            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);

        }


        [Test, Combinatorial]
        public void Lexi_Soft_Cost([Values(
         ITC_Comp01,
         ITC_Comp05,
         ITC_Comp06,
         ITC_Comp08,
         ITC_Comp12,
            ITC_Comp11,
         ITC_Comp18,
                        //more
         ITC_Comp02,
         ITC_Comp03,
         ITC_Comp04
            )] string filename, [Values(false)] bool SpecificQual)
        {
            var algoname = nameof(Lexi_Soft_Cost) + $"_SpecQual_{SpecificQual}";
         //   if (ConsoleToFile)
        //        SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
           // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                // UseStageIandII = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                ConstraintPenalty = null,
                UseRoomsAsTypes = true,
            };
            var solver = new MultiObjectiveSolver(data,_problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                ExtraTimeOnCornerPointsMultiplier = 8,
               UseSpecificObjConstraints = SpecificQual,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            double maxcost;
            int minSoft;
            int minSoftBound;
            solver.CalcLexMaxCostMinSoftcons(out maxcost, out minSoft,out minSoftBound);
            Console.WriteLine($"MinSoft: {minSoft} MaxCost: {maxcost}");

        }
        [Test, Combinatorial]
        public void Lexi_Cost_Soft([Values(
         ITC_Comp01,
         ITC_Comp05,
         ITC_Comp06,
         ITC_Comp08,
         ITC_Comp12,
            ITC_Comp11,
         ITC_Comp18,
            //more
         ITC_Comp02,
         ITC_Comp03,
         ITC_Comp04,
         ITC_Comp07

            )] string filename, [Values(false)] bool SpecificQual)
        {
            var algoname = nameof(Lexi_Cost_Soft) + $"_SpecQual_{SpecificQual}";
         //   if (ConsoleToFile)
        //        SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
           // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                // UseStageIandII = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                ConstraintPenalty = null,
                UseRoomsAsTypes = true,
            };
            var solver = new MultiObjectiveSolver(data,_problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                ExtraTimeOnCornerPointsMultiplier = 8,
               UseSpecificObjConstraints = SpecificQual,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            double minCost;
            int maxSoftConsViol;
            int minCostBound;
            solver.CalcLexMinCostMaxSoft(out minCost, out maxSoftConsViol, out minCostBound);
            Console.WriteLine($"MaxSoft: {maxSoftConsViol} MinCost: {minCost}");

        }
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void Lex_soft_cost_withTimeslot(string filename)
        {
            var algname = nameof(Lex_soft_cost_withTimeslot);
            //  formulation.AvailabilityHardConstraint = false;
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var newrooms = CreateRoomsFixedSize(data, 25, 5);
            data.SetRoomList(newrooms);

            var par = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                TuneGurobi = false,
                UseRoomsAsTypes = false,
            };
            var costmodel = new CCTModel(data, ProblemFormulation.MinimizeRoomCost, par);
            costmodel.Optimize();

           
            data.SetRoomList(costmodel.GetUsedRooms().Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList());
            

            //var formulation = ProblemFormulation.UD2NoOverbook;
            // formulation.AvailabilityHardConstraint = false;
            //   formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                // UseRoomsAsTypes = true,
                UseRoomsAsTypes = false,
                RoomBoundForEachTimeSlot = true,

            };
            var model = new CCTModel(data, _problemFormulation, ModelParameters);

            model.Optimize(300);
            var userooms = model.GetUsedRooms();
            Console.WriteLine($"Used rooms: {userooms.Sum(kv => kv.Value)} over {userooms.Count} ");

            // model.PenalizeRoomUsedMoreThanOnce(100);
            model.PenalizeRoomUsedMoreThanOnce(1, true);
            model.SetObjective(0, 0, 0.001);
            model.SetProximityOrigin();
            model.SetQualConstraint(model.ObjSoftCons);
            // model.SetProxConstraint(20);
            model.Fixsol(true);
            model.Optimize();
            model.Fixsol(false);


            model.Optimize(900);
            model.DisplayObjectives();
            var sol = new Solution(data, ProblemFormulation.MinimizeRoomCost);
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());

            File.AppendAllText(@"c:\temp\heuristic.txt", $"{algname};{filename};{model.ObjSoftCons}\n");
        }



        [Test]
        [TestCase(ITC_Comp01, 0.01)]
        [TestCase(ITC_Comp05, 2)]
        [TestCase(ITC_Comp06, 0.1)]
        [TestCase(ITC_Comp12, 0.5)]
        [TestCase(ITC_Comp18, 0.01)]
        [TestCase(ITC_Comp08, 0.1)]
        public void TestBudgetRelaxations(string filename, double stepsize)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);


            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
                TuneGurobi = true,
                SaveBoundInformation = true,
            });
            model.RelaxModel();

            model.SetObjective(0, 1, 0);
            model.Optimize();
            var min = model.Objective; // Math.Floor(model.Objective*100)/100;
            Console.WriteLine($"Minimum budget for relaxed: {min}");
            model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
                TuneGurobi = true,
                SaveBoundInformation = true,
            });

            //model.WriteModel(@"c:\temp\model.lp");
            var bounds = new List<Tuple<double, double,double,double>>();
            double prevobj = double.PositiveInfinity;
            int lasts = 5;
            for (double budget = min; budget < min*2; budget += stepsize)
            {

                model.RelaxModel();
                Console.WriteLine($"Budget:{budget : 0.00} ");
                model.SetBudgetConstraint(budget);
                if (model.Optimize(300))
                {
                    bounds.Add(Tuple.Create(budget, model.Objective, formulation.MinimumWorkingDaysWeight*model.ObjMWD, formulation.CurriculumCompactnessWeight*model.ObjCC)); //Save each objective? MWD CC
                    if (prevobj - model.Objective < 1e-6) lasts--;
                    if (lasts == 0) break;
                    prevobj = model.Objective;
                }
            }
            Console.WriteLine("Budget;Obj;MWD;CC");
            Console.WriteLine(string.Join("\n", bounds.Select(b => $"{b.Item1 : #.###};{b.Item2: #.###};{b.Item3: #.###};{b.Item4: #.###}")));
        }


        [Test]
        [TestCase(ITC_Comp18, 300)]
        [TestCase(ITC_Comp18, 325)]
        [TestCase(ITC_Comp18, 350)]
        [TestCase(ITC_Comp18, 400)]
        [TestCase(ITC_Comp18, 500)]
        [TestCase(ITC_Comp18, 600)]
        public void PlotNodeSols(string filename, double maxseats)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);

            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
             //   TuneGurobi = true,
                //TuneGurobi = false,
                SaveBoundInformation = true,
                //FocusOnlyOnBounds = true,
            });

           // model.SetObjective(0, 1, 0);
            //model.Optimize();
           // model.SetObjective(8, 1, 0); // Weighted sum
            model.SetObjective(1, 0, 0);
            model.SetBudgetConstraint(maxseats);
            model.Optimize(300);

   
            Console.WriteLine();
            Console.WriteLine("time;MWD;CC;softobj;obj;cost;bnd;");
            Console.WriteLine(String.Join("\n",
            model.bounds.Select(t => $"{t.Item6};{t.Item1};{t.Item2};{t.Item1+t.Item2};{t.Item3};{t.Item5};{t.Item4}")));
            Console.WriteLine($"Last bound: {model.ObjBound}");

        }

        [Test]
        [TestCase(ITC_Comp18)]
        public void ThresholdBehvaiour(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);

            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
             //   TuneGurobi = true,
                //TuneGurobi = false,
                SaveBoundInformation = true,
                //FocusOnlyOnBounds = true,
            });
            var seatscal = new RoomCombinations(data,25);

            var CriticalSeats = seatscal._minCost;
           // model.SetObjective(0, 1, 0);
            //model.Optimize();
           // model.SetObjective(8, 1, 0); // Weighted sum
            model.SetObjective(1, 0, 0);
            model.SetBudgetConstraint(CriticalSeats);
            model.Optimize(300);

   
            Console.WriteLine();
            Console.WriteLine("time;MWD;CC;softobj;obj;cost;bnd;");
            Console.WriteLine(String.Join("\n",
            model.bounds.Select(t => $"{t.Item6};{t.Item1};{t.Item2};{t.Item1+t.Item2};{t.Item3};{t.Item5};{t.Item4}")));
            Console.WriteLine($"Last bound: {model.ObjBound}");

        }




        [Test]
        [TestCase(ITC_Comp18, 300)]
        [TestCase(ITC_Comp18, 325)]
        [TestCase(ITC_Comp18, 350)]
        [TestCase(ITC_Comp18, 400)]
        [TestCase(ITC_Comp18, 500)]
        [TestCase(ITC_Comp18, 600)]
        public void PlotNodeSolsWeightedSum(string filename, int maxseats)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);

            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
             //   TuneGurobi = true,
                //TuneGurobi = false,
                SaveBoundInformation = true,
                //FocusOnlyOnBounds = true,
            });

           // model.SetObjective(0, 1, 0);
            //model.Optimize();
            model.SetObjective(4, 1, 0); // Weighted sum
            //model.SetObjective(1, 0, 0);
            //model.SetBudgetConstraint(maxseats);
            model.SetPossibleRoomCombinations(300,325);
         //   model.SetBudgetConstraint(325);
           // model.SetPossibleRoomCombinations(300);
         //   model.SetPossibleRoomCombinations(325);
            model.Optimize(300);

            Console.WriteLine($"Cost: {model.ObjCost} Softobj: {model.ObjSoftCons}");

   
            Console.WriteLine();
            Console.WriteLine("time;MWD;CC;softobj;obj;cost;bnd;");
            Console.WriteLine(String.Join("\n",
            model.bounds.Select(t => $"{t.Item6};{t.Item1};{t.Item2};{t.Item1+t.Item2};{t.Item3};{t.Item5};{t.Item4}")));
            Console.WriteLine($"Last bound: {model.ObjBound}");

        }

        [Test, Combinatorial]
        public void TimeslotMulti([Values(0, 1)] int formulationtype, [Values(
            ITC_Comp01,
            ITC_Comp06,
            ITC_Comp08,
            ITC_Comp11,
            ITC_Comp18
            )] string filename)
        {

            ProblemFormulation formulation;
            var secondobj = "";
            if (formulationtype == 0)
            {
                formulation = ProblemFormulation.UD2NoOverbook;
                secondobj = nameof(ProblemFormulation.UD2NoOverbook);
            }
            else
            {
                formulation = ProblemFormulation.MinimizeRoomCost;
                secondobj = nameof(ProblemFormulation.MinimizeRoomCost);
            }


            var algoname = nameof(TimeslotMulti) + "_" + secondobj;

            var data = Data.ReadXml(dataPath + filename, "120", "4");
            CreateExtraTimeslotEachDay(data, 5);

            var solver = new MultiTimeslotSolver(60, data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
                TuneGurobi = true,
                //TuneGurobi = false,
                SaveBoundInformation = false,
                //FocusOnlyOnBounds = true,
            });
            solver.formulationtype = formulationtype;
            var solutions = solver.Run();

            Console.WriteLine("timeslots;obj;LB;s");
            Console.WriteLine(String.Join("\n", solutions.Select(t => $"{t.Item1};{t.Item2};{t.Item3};{t.Item4}")));

        }
    }
}