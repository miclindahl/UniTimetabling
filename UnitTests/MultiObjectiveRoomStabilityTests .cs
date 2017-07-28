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
    internal class MultiObjectiveRoomStabilityTests : UnitTestBase
    {
        private bool ConsoleToFile = true;
        private int Timelimit = 15 * 60;


        [Test]
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void MultiObjectiveRoomStabilityFixedTime(string filename)
        {
            var algoname = nameof(MultiObjectiveRoomStabilityFixedTime);
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");


            var data = Data.ReadXml(dataPath + filename, "120", "4");

            var newrooms = CreateRoomsFixedSize(data, 25, 3);
            data.SetRoomList(newrooms);

            var formulation = ProblemFormulation.MinimizeRoomCost;
           // formulation.OverbookingAllowed = 0.25;

            var startmodel = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
            });

            startmodel.Optimize(300);
            var fixedTimeSlotAssignments = startmodel.GetAssignments();

            var stageIsol = new Solution(data,formulation);
            stageIsol.SetAssignments(fixedTimeSlotAssignments);
            Console.WriteLine("Assignments of timeslots:");
            Console.WriteLine(stageIsol.AnalyzeSolution());

            //  var newrooms = CreateRooms(data,5);
            // var newrooms = CreateRoomsFixedSize(data, 20);fr


            var problemFormulation = ProblemFormulation.EverythingZero;
            problemFormulation.RoomStabilityWeight = 1;
            //problemFormulation.OverbookingAllowed = 1;
            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = true,
                // UseStageIandII = true,
                UseHallsConditions = false,
                UseRoomHallConditions = false,
                NSols = 100,
                ConstraintPenalty = 50,
                UseRoomsAsTypes = false,

            };
            var solver = new MultiObjectiveSolver(data, problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                MIPGap = 0.0,
                Steps = 10,
                LocalBranching = 0,
                EpsilonOnQuality = false,
                FixedTimeSlotAssignments = fixedTimeSlotAssignments
            };
            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);

            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data, problemFormulation, algoname, DateTime.Now.ToString(), pareto);

        }

     
    }
}