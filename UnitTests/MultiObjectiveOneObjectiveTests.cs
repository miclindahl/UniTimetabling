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
    internal class MultiObjectiveOneObjectiveTests : UnitTestBase
    {
        private int Timelimit = 15*60;

        private bool ConsoleToFile = true;
     

        [Test, Combinatorial]
        public void MultiObjOneObjetive([Values(
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
            )]
        string filename, 
            [Values(ProblemFormulation.Objective.MinimumWorkingDaysWeight, 
            ProblemFormulation.Objective.CurriculumCompactnessWeight,
            ProblemFormulation.Objective.BadTimeslots,
            ProblemFormulation.Objective.StudentMinMaxLoadWeight)]
        ProblemFormulation.Objective objective)
        {
            //ProblemFormulation.Objective objective = ProblemFormulation.Objective.BadTimeslots;
            
            var algoname = nameof(MultiObjOneObjetive) + "_" + Enum.GetName(objective.GetType(), objective);
            if (ConsoleToFile)
                SetConsoleOutputToFile(@"c:\temp\multiobjective\\logs\\" + filename + "\\" + algoname + ".log");

            var data = Data.ReadXml(dataPath + filename, "120", "4");
            //var newrooms = CreateRooms(data);
             var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);

            var problemFormulation = ProblemFormulation.EverythingZero;
            problemFormulation.SetWeight(objective,1);
            //problemFormulation.AvailabilityHardConstraint = false;

            var mipModelParameters = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                // UseStageIandII = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                //ConstraintPenalty = null, //1,
                ConstraintPenalty = 50,
                UseRoomsAsTypes = true,
            };

            var solver = new MultiObjectiveSolver(data,problemFormulation, mipModelParameters)
            {
                Timelimit = Timelimit,
                EpsilonOnQuality = true,
                EpsilonRelaxing = false,
            };

            Console.WriteLine("Algorithm: " + algoname);
            DisplayParameterObject(problemFormulation);
            DisplayParameterObject(solver);
            DisplayParameterObject(solver.MipParameters);
            var pareto = solver.Run();
            WriteSol(@"c:\temp\multiobjective\", filename, data,problemFormulation ,algoname, DateTime.Now.ToString(), pareto);
        }
     
    }
}