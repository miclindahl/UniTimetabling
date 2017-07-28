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
    internal class MultiObjectiveTests : UnitTestBase
    {
     //   private int Timelimit = 60*5;//*60;
        private int Timelimit = 60*15;//*60;

        private bool ConsoleToFile = true;
        private ProblemFormulation _problemFormulation = ProblemFormulation.UD2NoOverbook;// ProblemFormulation.UD2NoNoOverbookNoUnAvilability;

     
        
        [Test,Combinatorial]
     //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void MultiObjSolveEpsilon([Values(
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
            [Values(false,true)] bool EpsilonOnQuality,
            [Values(false)] bool EpsilonRelaxing,
            [Values(false)] bool DoubleSweep)
        {
            var algoname = nameof(MultiObjSolveEpsilon) + $"EpsOnQual_{EpsilonOnQuality}_EpsRelax_{EpsilonRelaxing}_doublesweep_{DoubleSweep}";
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
                ScalePenaltyWithCourseSize = false,
                
            };
            var solver = new MultiObjectiveSolver(data,_problemFormulation, mipModelParameters)
            {
                Timelimit = (DoubleSweep ? Timelimit/2 : Timelimit),
                EpsilonOnQuality = EpsilonOnQuality,
                EpsilonRelaxing = EpsilonRelaxing,
                ExtraTimeOnCornerPointsMultiplier = 2,
                DoubleSweep = DoubleSweep,
                Steps = 10, //TODO: only zero steps
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);
        }        



        [Test,Combinatorial]
     //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void MultiObjSolveWeighted([Values(
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
         ITC_Comp21)] string filename,
               [Values(false)] bool classSizeWeightedPenalty)
        {
            var algoname = nameof(MultiObjSolveEpsilon) + $"Weighted_classsizepenalty_{classSizeWeightedPenalty}";
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
                ScalePenaltyWithCourseSize = classSizeWeightedPenalty,
            };
            var solver = new MultiObjectiveSolver(data,_problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                UseEpsilonMethod = false,
                ExtraTimeOnCornerPointsMultiplier = 2,
                DoubleSweep = false,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);
        }

             [Test,Combinatorial]
     //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void MultiObjSolveExhausted([Values(
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
         ITC_Comp21)] string filename)
        {
            var algoname = nameof(MultiObjSolveExhausted) +  "stdgurobi";
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
           // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = false, //notice
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
                EpsilonOnQuality = false,
                ExtraTimeOnCornerPointsMultiplier = 1,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.RunExhaustiveMethod();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);
        }
          [Test,Combinatorial]
     //   [TestCaseSource(nameof(TestMultiObjTestBed))]
        public void MultiObj3index([Values(
         ITC_Comp02)] string filename)
        {
            var algoname = nameof(MultiObj3index) +  "3idx";
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
           // var newrooms = CreateRooms(data);
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = false, //notice
                //UseStageIandII = false,
                 UseStageIandII = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                ConstraintPenalty = null,
                UseRoomsAsTypes = true,
            };
           var solver = new MultiObjectiveSolver(data, ProblemFormulation.UD2, mipModelParameters)
            {
                Timelimit = 60,
                EpsilonOnQuality = false,
                ExtraTimeOnCornerPointsMultiplier = 1,
            };
      
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(_problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.RunExhaustiveMethod();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), pareto);
        }

        
        [Test, Combinatorial]
        public void TimeslotMulti([Values(0, 1)] int formulationtype, 
            [Values(
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
            )] string filename)
        {
            ProblemFormulation formulation;
            var secondobj = "";
            if (formulationtype == 0)
            {
                formulation = ProblemFormulation.UD2NoOverbook;
                secondobj = nameof(ProblemFormulation.UD2NoOverbook);
                //this should use std room.
            }
            else
            {
                formulation = ProblemFormulation.MinimizeRoomCost;
                secondobj = nameof(ProblemFormulation.MinimizeRoomCost);
            }

            
            var algoname = nameof(TimeslotMulti) + "_" + secondobj+ "_stdgurobi";

            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");


            var data = Data.ReadXml(dataPath + filename, "120", "4");
            if (formulationtype == 1)
            {
                var newrooms = CreateRoomsFixedSize(data, 25, 1);
                data.SetRoomList(newrooms);
            }

            CreateExtraTimeslotEachDay(data, 5);

            var solver = new MultiTimeslotSolver(Timelimit, data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = formulationtype == 0 ? false : true,
                UseStageIandII = false,
               // TuneGurobi = true,
                TuneGurobi = false,
                SaveBoundInformation = false,
                //FocusOnlyOnBounds = true,

            });
            solver.formulationtype = formulationtype;
            var solutions = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, _problemFormulation, algoname, DateTime.Now.ToString(), solutions);

            Console.WriteLine("timeslots;obj;LB;s");
            Console.WriteLine(String.Join("\n", solutions.Select(t => $"{t.Item1};{t.Item2};{t.Item3};{t.Item4}")));
        }

    }
}