using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversityTimetabling.MIPModels;

namespace UniversityTimetabling
{
    public class Heuristics
    {
        private int TimelimitStageI;
        private Data data;
        private ProblemFormulation formulation;
        private CCTModel _model;
        private CCTModel.MIPModelParameters MIPmodelParameters;
        private Solution _solution;
        public int Timetotighten = 20;
        public int totalseconds;
        public int roomtightseconds;

        public Heuristics(int timelimit, Data data, ProblemFormulation formulation, CCTModel.MIPModelParameters mipModelParameters)
        {
            TimelimitStageI = timelimit;
            this.data = data;
            this.formulation = formulation;
            this.MIPmodelParameters = mipModelParameters;
        }

        
        public Solution Run()
        {
            var stopwatch = Stopwatch.StartNew();

            var model = new CCTModel(data, formulation, MIPmodelParameters);

            model.Optimize(TimelimitStageI - Timetotighten);
            var userooms = model.GetUsedRooms();
            Console.WriteLine($"Used rooms: {userooms.Sum(kv => kv.Value)} over {userooms.Count} ");
            Console.WriteLine($"OveruseOnTimeslots: {model.GetOverUseOfRooms()} ");

           //  model.PenalizeRoomUsedMoreThanOnce(100);
            model.PenalizeRoomUsedMoreThanOnce(10, true);

            model.SetObjective(0, 0, 0.01);
            //model.SetObjective(0, 0, 0);
            //model.SetMipHintToCurrent();
            model.SetProximityOrigin();
            model.SetQualConstraint(model.ObjSoftCons);
            
            model.Fixsol(true);
            model.Optimize();
            model.Fixsol(false);

        //    model.Optimize(timetotighten+50,cutoff:500);
            model.ModelParameters.AbortSolverOnZeroPenalty = true;
            var tighttimer = Stopwatch.StartNew();
            model.Optimize(Timetotighten+300);
            tighttimer.Stop();
            model.DisplayObjectives();
            _solution = new Solution(data, formulation);
            _solution.SetAssignments(model.GetAssignments());
            
            //timers
            stopwatch.Stop();
            tighttimer.Stop();
            totalseconds = (int)stopwatch.Elapsed.TotalSeconds;
            roomtightseconds = (int) tighttimer.Elapsed.TotalSeconds;
            return _solution;
        }
        public Solution RunUnavailbility()
        {
            var stopwatch = Stopwatch.StartNew();

            var model = new CCTModel(data, formulation, MIPmodelParameters);

            model.Optimize(TimelimitStageI - Timetotighten);
            Console.WriteLine($"Unavailableused: {model.GetUnavailableused()} ({(double) model.GetUnavailableused()/data.Courses.Sum(c => c.Lectures):0.00%})");

            model.PenalizeUnavailability(10);

           // model.SetObjective(0, 0, 0.01);
           // model.SetObjective(0, 0, 0);
            //model.SetMipHintToCurrent();
            model.SetProximityOrigin();
           // model.SetQualConstraint(model.ObjSoftCons);
            
            model.Fixsol(true);
            model.Optimize();
            model.Fixsol(false);

        //    model.Optimize(timetotighten+50,cutoff:500);
       //     model.ModelParameters.AbortSolverOnZeroPenalty = true;
            var tighttimer = Stopwatch.StartNew();
            model.Optimize(Timetotighten+900);
            tighttimer.Stop();
            model.DisplayObjectives();
            _solution = new Solution(data, formulation);
            _solution.SetAssignments(model.GetAssignments());

            Console.WriteLine($"Unavailableused: {model.GetUnavailableused()} ({(double)model.GetUnavailableused() / data.Courses.Sum(c => c.Lectures):0.00%})");

            //timers
            stopwatch.Stop();
            tighttimer.Stop();
            totalseconds = (int)stopwatch.Elapsed.TotalSeconds;
            roomtightseconds = (int) tighttimer.Elapsed.TotalSeconds;

            return _solution;
        }

        public Solution SolveStageII()
        {
            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseStageIandII = true,
            });
            model.SetAndFixSolution(_solution._assignments.ToList());
            //model.MipStart(_solution._assignments.ToList());
            model.Optimize(1200);
            _solution.SetAssignments(model.GetAssignments());
            return _solution;
        }

        public Solution RunLargeCoursesFirst()
        {
            var stopwatch = Stopwatch.StartNew();

            var cutlimit = 0.1;
            var ncourses = 10;//(int) 0.1*data.Courses.Count;
            var newcourses = data.Courses.OrderByDescending(c => c.NumberOfStudents).Take(ncourses).ToList();
            var remcourse = data.Courses.Except(newcourses).ToList();

            var newdata = new Data(data.Courses,data.Lecturers,data.Rooms,data.Curricula,data.TimeSlots,data.Days,data.Periods,data.MinimumPeriodsPerDay,data.MaximumPeriodsPerDay);
            newdata.CleanRemovedCourses(remcourse);

            Console.WriteLine($"Solving with {newcourses.Count} ({(double) newcourses.Count/data.Courses.Count : 0.00%})");
            var modelpre = new CCTModel(newdata, formulation, MIPmodelParameters);

            modelpre.Optimize();
            var ass = modelpre.GetAssignments();

            var model = new CCTModel(data, formulation, MIPmodelParameters);
           // model.SetAndFixSolution(ass);

            model.Optimize();

            //    model.Optimize(timetotighten+50,cutoff:500);
            //     model.ModelParameters.AbortSolverOnZeroPenalty = true;
            var tighttimer = Stopwatch.StartNew();
            model.Optimize(Timetotighten + 900);
            tighttimer.Stop();
            model.DisplayObjectives();
            _solution = new Solution(data, formulation);
            _solution.SetAssignments(model.GetAssignments());

            Console.WriteLine($"Unavailableused: {model.GetUnavailableused()} ({(double)model.GetUnavailableused() / data.Courses.Sum(c => c.Lectures):0.00%})");

            //timers
            stopwatch.Stop();
            tighttimer.Stop();
            totalseconds = (int)stopwatch.Elapsed.TotalSeconds;
            roomtightseconds = (int)tighttimer.Elapsed.TotalSeconds;

            return _solution;
        }
    }
}
