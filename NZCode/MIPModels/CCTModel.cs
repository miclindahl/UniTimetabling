using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using UniversityTimetabling.StrategicOpt;

namespace UniversityTimetabling.MIPModels
{
    public class CCTModel
    {
        public ConcurrentQueue<Tuple<double,double,double,double,double,double>> bounds = new ConcurrentQueue<Tuple<double,double, double,double,double,double>>(); 
        private GRBModel _model;
        private GRBEnv _env;
        public double Objective
        {
            get
            {
                try
                {
                    return _model.Get(GRB.DoubleAttr.ObjVal);
                }
                catch
                {
                    return double.MaxValue;
                }
            }
        }


        public List<Tuple<double,double>> Relaxations = new List<Tuple<double, double>>(); 

        public double ObjBound => _model.Get(GRB.DoubleAttr.ObjBound);

        private Dictionary<Tuple<Course, TimeSlot, Room>, GRBVar> varX;
        private Dictionary<Tuple<Course, TimeSlot>, GRBVar> varY;
        private Dictionary<Room, GRBVar> varR;
        private GRBConstr _budgetConstraint;
        private GRBConstr _qualityConstraint;
        private GRBConstr _localBranchingConstraint;
        public GRBVar _softConstraints;
        public GRBVar _cost;
        private GRBVar _proximity;
        public MIPModelParameters ModelParameters { get; private set; }

        public class MIPModelParameters
        {
            public bool UseHallsConditions { get; set; }
            public bool UseRoomHallConditions { get; set; }
            public bool UseStageIandII { get; set; }
            public bool TuneGurobi { get; set; }
            public int NSols { get; set; }
            public double? ConstraintPenalty { get; set; }
            public bool AbortSolverOnZeroPenalty { get; set; }
            public bool UseRoomsAsTypes { get; set; }
            public bool SaveRelaxations { get; set; }
            public int Seed { get; set; }
            public bool SaveBoundInformation { get; set; }

            public bool ScalePenaltyWithCourseSize { get; set; } = false;
            public bool RoomBoundForEachTimeSlot { get; set; } = false;
            public bool FocusOnlyOnBounds { get; set; } = false;

            public MIPModelParameters()
            {
                UseHallsConditions = true;
                UseRoomHallConditions = true;
                UseStageIandII = true;
                TuneGurobi = false;
                NSols = 1;
                ConstraintPenalty = null;
                AbortSolverOnZeroPenalty = false;
                UseRoomsAsTypes = false;
                SaveRelaxations = false;
                Seed = 60;
                SaveBoundInformation = false;
                RoomBoundForEachTimeSlot = false;
                FocusOnlyOnBounds = false;

            }
        }


        private Dictionary<Curriculum, Dictionary<int, GRBVar>> _studentLoadViol;
        private Dictionary<Curriculum, Dictionary<int, GRBVar>> _studentLoad;
        private GRBVar _roomStability;
        private GRBVar _studentminmaxload;
        private GRBConstr _proxCalc;
        public GRBVar ConsPenalty;
        public Dictionary<Tuple<RoomCapacity,TimeSlot>, GRBVar> _roomOverUsePenalty;
        private Dictionary<Curriculum, Dictionary<TimeSlot, GRBVar>> _varcurrAssigned;
        public GRBVar _minimumworkingdaysbelow;
        public GRBVar _varCurrCompactViol;
        public GRBVar _varBadTimeSlotsViol;
        private Dictionary<RoomCapacity, GRBVar> _varRplus;
        private Dictionary<Course, GRBVar> _varDaysBelow;
        private Dictionary<Curriculum, Dictionary<TimeSlot, GRBVar>> _varcurrAlone;
        private Dictionary<Tuple<Course, int>, GRBVar> _varDayinUse;
        private GRBVar _unsuitableRooms;
        private Dictionary<TimeSlot, GRBVar> _varTimeslotUsed;
        private GRBVar _vartotaltimeslotsused;

        public int ObjSoftCons => (int) Math.Round(_softConstraints.Get(GRB.DoubleAttr.X));

        public double ObjCost => _cost.Get(GRB.DoubleAttr.X);
        public double ObjMWD => _minimumworkingdaysbelow.Get(GRB.DoubleAttr.X);
        public double ObjCC => _varCurrCompactViol.Get(GRB.DoubleAttr.X);

        public int RunTime => (int) _model.Get(GRB.DoubleAttr.Runtime);

        public Data Data { get; private set; }
        public ProblemFormulation ProblemFormulation { get; private set; }

        public CCTModel(Data data, ProblemFormulation formulation, MIPModelParameters parameters = null)
        {
            Data = data;
            ProblemFormulation = formulation;
            ModelParameters = parameters ?? new MIPModelParameters();
            BuildMIPModel();
        }

        private void BuildMIPModel()
        {
            _env = new GRBEnv();
            _model = new GRBModel(_env);
            varY = new Dictionary<Tuple<Course, TimeSlot>, GRBVar>();

            foreach (var course in Data.Courses)
            {
                foreach (var timeslot in Data.TimeSlots)
                {
                    var feasibleTime = !(ProblemFormulation.AvailabilityHardConstraint && course.UnavailableTimeSlots.Contains(timeslot));

                    varY[Tuple.Create(course, timeslot)] = _model.AddVar(0, feasibleTime ? 1 : 0, 0, GRB.BINARY,
                            $"y_{course}_{timeslot}");
                }
            }
     
            //room used.
            varR = new Dictionary<Room, GRBVar>();
            if (ModelParameters.RoomBoundForEachTimeSlot)
            {
                _roomOverUsePenalty = new Dictionary<Tuple<RoomCapacity,TimeSlot>, GRBVar>();
                foreach (var timeSlot in Data.TimeSlots)
                {
                    foreach (var roomCapacity in Data.RoomCapacities)
                    {
                        _roomOverUsePenalty[Tuple.Create(roomCapacity,timeSlot)] = _model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER,
                            $"rp_{roomCapacity}_{timeSlot}");
                    }
                }
            }

            foreach (var room in Data.Rooms)
            {
                varR[room] = _model.AddVar(0, ModelParameters.UseRoomsAsTypes ? GRB.INFINITY : 1, 0, GRB.INTEGER,
                            $"r_{room}");
            //    _roomOverUsePenalty[room] = _model.AddVar(0, 0, ModelParameters.ConstraintPenalty.Value, GRB.CONTINUOUS,
            //                $"rPeanlty_{room}");
            }
            _varRplus = new Dictionary<RoomCapacity, GRBVar>();
            foreach (var roomCapacity in Data.RoomCapacities)
            {
                _varRplus[roomCapacity] = _model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER,
                            $"rplus_{roomCapacity}");
            }

            //DaysInUse
            _varDayinUse = new Dictionary<Tuple<Course,int>,GRBVar>();
            foreach (var course in Data.Courses)
            {
                foreach (var day in Data.TimeSlotsByDay)
                {
                    _varDayinUse[Tuple.Create(course,day.Key)] = _model.AddVar(0, 1, 0, GRB.CONTINUOUS,
                            $"dayinuse_{course}_{day.Key}");
                }
            }

            //minimumbelow
            _varDaysBelow = new Dictionary<Course,GRBVar>();
            foreach (var course in Data.Courses)
            {
                _varDaysBelow[course] = _model.AddVar(0, course.MinimumWorkingDays,0, GRB.CONTINUOUS,
                            $"daysbelow_{course}");
            }


            _varTimeslotUsed = new Dictionary<TimeSlot,GRBVar>();
            foreach (var ts in Data.TimeSlots)
            {
                _varTimeslotUsed.Add(ts,_model.AddVar(0,1,0,GRB.BINARY,$"TimeslotsUsed_{ts}"));
            }
            _vartotaltimeslotsused = _model.AddVar(0, Data.TimeSlots.Count, 0, GRB.CONTINUOUS, "timeslotsused");


            _roomStability = _model.AddVar(0, Data.Courses.Count*(Data.Rooms.Count - 1), 0,
                GRB.CONTINUOUS,"roomStability");

            _unsuitableRooms = _model.AddVar(0,Data.Courses.Sum(c => c.Lectures), 0,
                GRB.CONTINUOUS,"UnsuitableRooms");



            _varcurrAlone = new Dictionary<Curriculum, Dictionary<TimeSlot, GRBVar>>();
            _varcurrAssigned = new Dictionary<Curriculum, Dictionary<TimeSlot, GRBVar>>();
            foreach (var curriculum in Data.Curricula)
            {
                _varcurrAlone[curriculum] = new Dictionary<TimeSlot, GRBVar>();
                _varcurrAssigned[curriculum] = new Dictionary<TimeSlot, GRBVar>();
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _varcurrAlone[curriculum][timeSlot] = _model.AddVar(0, 1, 0,
                        GRB.CONTINUOUS, $"alone_{curriculum}_{timeSlot}");
                    _varcurrAssigned[curriculum][timeSlot] = _model.AddVar(0, 1, 0,
                        GRB.CONTINUOUS, $"assigned_{curriculum}_{timeSlot}");
                }
            }
            _softConstraints = _model.AddVar(0, GRB.INFINITY, 1, GRB.CONTINUOUS,"SoftConstraints");
            _cost = _model.AddVar(0, GRB.INFINITY, ProblemFormulation.RoomCostWeight, GRB.CONTINUOUS,"costs");

            _studentLoad = new Dictionary<Curriculum,Dictionary<int,GRBVar>>();
            _studentLoadViol = new Dictionary<Curriculum,Dictionary<int,GRBVar>>();
            var _CurrDayInUse = new Dictionary<Curriculum,Dictionary<int,GRBVar>>();
            foreach (var curr in Data.Curricula)
            {
                _studentLoad[curr] = new Dictionary<int, GRBVar>();
                _studentLoadViol[curr] = new Dictionary<int, GRBVar>();
                _CurrDayInUse[curr] = new Dictionary<int, GRBVar>();
                foreach (var day in Data.TimeSlotsByDay)
                {
                    _CurrDayInUse[curr][day.Key] = _model.AddVar(0, 1, 0, GRB.CONTINUOUS,
                        $"curdayinuse_{curr}_{day.Key}");
                    _studentLoad[curr][day.Key] = _model.AddVar(0, day.Value.Count, 0, GRB.CONTINUOUS,
                        $"studentload_{curr}_{day.Key}");
                    _studentLoadViol[curr][day.Key] = _model.AddVar(0, day.Value.Count, 0, GRB.CONTINUOUS,
                        $"studentloadViol_{curr}_{day.Key}");
                }
            }

            _studentminmaxload = _model.AddVar(0, GRB.INFINITY, 0,
                GRB.CONTINUOUS, "StudentMinMaxLoad");
            _minimumworkingdaysbelow = _model.AddVar(0, GRB.INFINITY, 0,
                GRB.CONTINUOUS, "MinimumWorkingDaysBelow");
            _varCurrCompactViol = _model.AddVar(0, GRB.INFINITY, 0,
                GRB.CONTINUOUS, "CurriculumCompactnessViol");
            _varBadTimeSlotsViol = _model.AddVar(0, GRB.INFINITY, 0,
                GRB.CONTINUOUS, "BadTimeslotsViol");

            var cpUB = ModelParameters.ConstraintPenalty == null ? 0 : GRB.INFINITY;
            ConsPenalty = _model.AddVar(0, cpUB, ModelParameters.ConstraintPenalty ?? 0,
                GRB.CONTINUOUS, "ConsPenalty");

           
            _proximity = _model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "proximity");

            _model.Update();
            if (ModelParameters.UseStageIandII)
            {

                AddRoomAssignment();
            }
            //Plan all
            foreach (var course in Data.Courses)
            {
                _model.AddConstr(varY.Where(t => t.Key.Item1.Equals(course)).Sumx(d => d.Value), GRB.EQUAL, course.Lectures,
                   $"PlanAlly_{course}");
            }

     
            //Additional room cuts
            _model.AddConstr(Data.TimeSlots.Count * varR.Values.Sumx(), GRB.GREATER_EQUAL, Data.Courses.Sum(c => c.Lectures), "TotalRooms");

            foreach (var roomCapacity in Data.RoomCapacities)
            { 
                _model.AddConstr(
                       varR.Where(r => r.Key.Capacity > roomCapacity.Value).Sumx(r => r.Value),
                       GRB.EQUAL,_varRplus[roomCapacity],
                       $"rpluscalc_{roomCapacity.Value}");
            }
           
            if (ModelParameters.UseRoomHallConditions)
            {
                foreach (var roomCapacity in Data.RoomCapacities)
                {
                    var extrapenalty = (ModelParameters.RoomBoundForEachTimeSlot ? _roomOverUsePenalty.Where(kv => kv.Key.Item1.Equals(roomCapacity)).Sumx(v => v.Value) : 0);
                    _model.AddConstr(
                        _varRplus[roomCapacity] * Data.TimeSlots.Count+ extrapenalty,
                        GRB.GREATER_EQUAL,
                        Data.Courses.Where(c => c.NumberOfStudents > roomCapacity.Value * (1+ProblemFormulation.OverbookingAllowed)).Sum(c => c.Lectures),
                        $"roomchosebound_{roomCapacity.Value}");
                }
            }
            if (ModelParameters.UseHallsConditions)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    //dont plan more than available rooms
                    var extrapenalty = (ModelParameters.RoomBoundForEachTimeSlot ? _roomOverUsePenalty.Where(kv => kv.Key.Item2.Equals(timeSlot)).Sumx(v => v.Value) : 0);
                    _model.AddConstr(
                        varY.Where(
                            v => v.Key.Item2.Equals(timeSlot))
                            .Sumx(v => v.Value), GRB.LESS_EQUAL,
                        varR.Sumx(r => r.Value)+extrapenalty,
                        $"roomcapcut");


                    foreach (var roomCapacity in Data.RoomCapacities)
                    {
                        var extrapen = (ModelParameters.RoomBoundForEachTimeSlot ? _roomOverUsePenalty[Tuple.Create(roomCapacity,timeSlot)] : (GRBLinExpr) 0);
                        _model.AddConstr(
                            varY.Where(
                                v =>
                                    v.Key.Item2.Equals(timeSlot) &&
                                    v.Key.Item1.NumberOfStudents >
                                    roomCapacity.Value*(1 + 0*ProblemFormulation.OverbookingAllowed))
                                .Sumx(v => v.Value), GRB.LESS_EQUAL,
                            _varRplus[roomCapacity]+ extrapen, //does not allow overbooking
                            $"roomcapcut_{timeSlot}_{roomCapacity.Value}");
                    }
                }
            }

            //Lecturers
            foreach (var lec in Data.Lecturers)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    
                    _model.AddConstr(lec.Courses.Select(c => 1*varY[Tuple.Create(c,timeSlot)]).Sumx(), GRB.LESS_EQUAL, 1,
                    $"Lec_{lec}_{timeSlot}");
                }
            }

            //no need to add if no curr

            //Curriculum
            foreach (var curr in Data.Curricula)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _model.AddConstr(curr.Courses.Select(c => 1 * varY[Tuple.Create(c, timeSlot)]).Sumx(), GRB.LESS_EQUAL, 1,
                    $"Curr_{curr}_{timeSlot}");
                }
            }
            //no need to add if no MWD

            //Min working days
            foreach (var course in Data.Courses)
            {
                foreach (var day in Data.TimeSlotsByDay)
                {
                    _model.AddConstr(day.Value.Select(t => varY[Tuple.Create(course,t)]).Sumx(), GRB.GREATER_EQUAL, _varDayinUse[Tuple.Create(course,day.Key)],
                  $"dayinuse_{course}_{day.Key}");
                }
            }
            //Calc daysbelow
            foreach (var course in Data.Courses)
            {
               _model.AddConstr(_varDayinUse.Where(t => t.Key.Item1.Equals(course)).Select(d => d.Value).Sumx() + _varDaysBelow[course],GRB.GREATER_EQUAL,course.MinimumWorkingDays,
                  $"daysbelow_{course}");
            }
            
         
            //Curriculm Compactness

            var varyByTimeslot = varY.GroupBy(y => y.Key.Item2).ToDictionary(y => y.Key);

            foreach (var curriculum in Data.Curricula)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _model.AddConstr(_varcurrAssigned[curriculum][timeSlot], GRB.EQUAL, varyByTimeslot[timeSlot].Where(t => curriculum.Courses.Contains(t.Key.Item1)).Select(d => d.Value).Sumx(), $"currAss_{curriculum}_{timeSlot}");

                    _model.AddConstr(_varcurrAlone[curriculum][timeSlot], GRB.GREATER_EQUAL,
                        _varcurrAssigned[curriculum][timeSlot] -
                        _varcurrAssigned[curriculum].Where(d => TimeSlot.TimeSlotsAreConsequtive(d.Key, timeSlot))
                            .Select(d => d.Value)
                            .Sumx(), $"currAlone_{curriculum}_{timeSlot}");

                }
            }
            //Student load
            foreach (var curriculum in Data.Curricula)
            {
                foreach (var day in Data.TimeSlotsByDay)
                {
                    foreach (var timeSlot in day.Value)
                    {

                        _model.AddConstr(_CurrDayInUse[curriculum][day.Key], GRB.GREATER_EQUAL,
                            _varcurrAssigned[curriculum][timeSlot],
                            $"currdayinuse_{curriculum}_{day}_{timeSlot}");
                    }


                    _model.AddConstr(_varcurrAssigned[curriculum].Where(t => day.Value.Contains(t.Key)).Sumx(t => t.Value), GRB.EQUAL, _studentLoad[curriculum][day.Key],
                        $"studentload{curriculum}_{day.Key}");
                    //limits
                    _model.AddConstr(_studentLoad[curriculum][day.Key] + _studentLoadViol[curriculum][day.Key], 
                        GRB.GREATER_EQUAL, Data.MinimumPeriodsPerDay*_CurrDayInUse[curriculum][day.Key],
                        $"minload_{curriculum}_{day.Key}");
                    _model.AddConstr(_studentLoad[curriculum][day.Key] - _studentLoadViol[curriculum][day.Key],
                        GRB.LESS_EQUAL, Data.MaximumPeriodsPerDay,
                        $"maxload_{curriculum}_{day.Key}");

                }
            }
            
            //timeslotinuse
            foreach (var timeSlot in Data.TimeSlots)
            {
                foreach (var course in Data.Courses)
                {
                    _model.AddConstr(varY[Tuple.Create(course, timeSlot)], GRB.LESS_EQUAL, _varTimeslotUsed[timeSlot],
                        $"timeslotused_{timeSlot}_{course}");
                }
            }
            
            //Consecutive timeslots
            var timeslotlist = Data.TimeSlots.OrderBy(ts => ts.Period).ThenBy(ts => ts.Day).ToList();
            for (var i = 0; i < timeslotlist.Count-1; i++)
            {
                     _model.AddConstr(_varTimeslotUsed[timeslotlist[i + 1]] - _varTimeslotUsed[timeslotlist[i]], GRB.LESS_EQUAL, 0,
                        $"timeslotconsec_{i}");
            }
         
            //Sum of timeslots
            _model.AddConstr(_varTimeslotUsed.Sumx(v => v.Value), GRB.EQUAL, _vartotaltimeslotsused, "calctotaltimeslots");
            
            //objcalc
          
            _model.AddConstr(_studentminmaxload, GRB.EQUAL, _studentLoadViol.Values.SelectMany(d => d.Values).Sumx(), "studentminmaxload");

            if (!ModelParameters.ScalePenaltyWithCourseSize)
            {
                _model.AddConstr(_minimumworkingdaysbelow, GRB.EQUAL, _varDaysBelow.Values.Sumx(v => v),
                    "minimumworkingdays");
                _model.AddConstr(_varCurrCompactViol, GRB.EQUAL,
                    _varcurrAlone.Values.SelectMany(d => d.Values).Sumx(v => 1*v), "currcompactness");
            }
            else
            {
                _model.AddConstr(_minimumworkingdaysbelow, GRB.EQUAL, _varDaysBelow.Sumx(v => v.Key.NumberOfStudents*v.Value),
                   "minimumworkingdays");
                _model.AddConstr(_varCurrCompactViol, GRB.EQUAL, _varcurrAlone.Select(d => d.Value.Values.Sumx(v => d.Key.NumberOfEstimatedStudents*v)).Sumx(exp => exp), "currcompactness");

            }


            var roomCapacityobj = ModelParameters.UseStageIandII ? varX.Sumx(x => Math.Max(x.Key.Item1.NumberOfStudents - x.Key.Item3.Capacity, 0)*x.Value) : (GRBLinExpr) 0;
            _model.AddConstr(_varBadTimeSlotsViol, GRB.EQUAL, varY.Sumx(v => v.Key.Item2.Cost * v.Value), "badtimeslots");

           
            var softconstraintCalc = 0
                                     + ProblemFormulation.RoomCapacityWeight*roomCapacityobj
                                     + ProblemFormulation.MinimumWorkingDaysWeight*_minimumworkingdaysbelow
                                     + ProblemFormulation.RoomStabilityWeight*_roomStability
                                     + ProblemFormulation.CurriculumCompactnessWeight*_varCurrCompactViol
                                     + ProblemFormulation.StudentMinMaxLoadWeight*_studentminmaxload
                                     + ProblemFormulation.BadTimeslotsWeight* _varBadTimeSlotsViol
                                     + ProblemFormulation.UnsuitableRoomsWeight*_unsuitableRooms;
                ;


            _model.AddConstr(_softConstraints, GRB.EQUAL, softconstraintCalc, "softconstraintCalc");
          
            _qualityConstraint = _model.AddConstr(_softConstraints - ConsPenalty, GRB.LESS_EQUAL, GRB.INFINITY,"qualityCons");
            _budgetConstraint = _model.AddConstr(_cost - ConsPenalty, GRB.LESS_EQUAL, ProblemFormulation.RoomBudget,
               "roombudget");
            _localBranchingConstraint = _model.AddConstr(_proximity -  ConsPenalty, GRB.LESS_EQUAL, GRB.INFINITY,
               "localbranchingConst");
            var costCalc = varR.Sumx(r => r.Key.Cost * r.Value);
            _model.AddConstr(_cost, GRB.EQUAL, costCalc, "costCalc");
           
            _model.Update();
            //Add callback
            _model.SetCallback(new CCTCallback(this));
        }

        private void AddRoomAssignment()
        {
            //room capacity constraint can be added here. 
            varX = new Dictionary<Tuple<Course, TimeSlot, Room>, GRBVar>();
            foreach (var course in Data.Courses)
            {
                foreach (var timeslot in Data.TimeSlots)
                {
                    var feasibleTime =
                        !(ProblemFormulation.AvailabilityHardConstraint && course.UnavailableTimeSlots.Contains(timeslot));
                    foreach (var room in Data.Rooms)
                    {
                        var feasibleAssign = course.NumberOfStudents <=
                                             room.Capacity*(1 + ProblemFormulation.OverbookingAllowed)
                            //allow 25% over booking &&
                            // &&  !course.UnsuitableRooms.Contains(room)
                            ;
                     
                        varX[Tuple.Create(course, timeslot, room)] = _model.AddVar(0, (feasibleTime && feasibleAssign) ? 1 : 0, 0,
                            GRB.BINARY,
                            $"x_{course}_{timeslot}_{room}");
                    }
                }
            }



            //roomUsedByCourse
            var varRoominUse = new Dictionary<Tuple<Course, Room>, GRBVar>();
            foreach (var course in Data.Courses)
            {
                foreach (var room in Data.Rooms)
                {
                    varRoominUse[Tuple.Create(course, room)] = _model.AddVar(0, 1, 0, GRB.CONTINUOUS,
                        $"roominuse_{course}_{room}");
                }
            }

            _model.Update();
            var xdic =
                varX.GroupBy(d => Tuple.Create(d.Key.Item1, d.Key.Item2))
                    .ToDictionary(d => Tuple.Create(d.Key.Item1, d.Key.Item2), d => d.Select(kp => kp.Value));
          
            //x and y connect.
            foreach (var course in Data.Courses)
            {
                foreach (var timeslot in Data.TimeSlots)
                {
                    _model.AddConstr(varY[Tuple.Create(course, timeslot)], GRB.EQUAL,
                        xdic[Tuple.Create(course, timeslot)].Sumx(),
                        $"xy_{course}_{timeslot}");
                }
                //planall

                _model.AddConstr(varX.Where(t => t.Key.Item1.Equals(course)).Sumx(d => d.Value), GRB.EQUAL, course.Lectures,
                    $"PlanAllx_{course}");
            }

            //Only use room once
            foreach (var room in Data.Rooms)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _model.AddConstr(
                        varX.Where(t => t.Key.Item2.Equals(timeSlot) && room.Equals(t.Key.Item3))
                            .Select(d => d.Value)
                            .Sumx(), GRB.LESS_EQUAL, varR[room],
                        $"Room_{room}_{timeSlot}");
                }
            }
            //roominuse
            foreach (var course in Data.Courses)
            {
                foreach (var room in Data.Rooms)
                {
                    foreach (var timeSlot in Data.TimeSlots)
                    {
                        _model.AddConstr(varRoominUse[Tuple.Create(course, room)], GRB.GREATER_EQUAL,
                            varX[Tuple.Create(course, timeSlot, room)], $"roominuse_{course}_{room}");
                    }
                }
            }

            //Unsuitablerooms
            //_unsuitableRooms
            _model.AddConstr(varX.Where(x => x.Key.Item1.UnsuitableRooms.Contains(x.Key.Item3)).Select(x => x.Value).Sumx(), GRB.EQUAL, _unsuitableRooms, "UnsuitableRoomsCalc");   

            _model.AddConstr(_roomStability, GRB.EQUAL, varRoominUse.Values.Sumx() - Data.Courses.Count, "roomstab");
        }




        public void SetBudgetConstraint(double? budget = null)
        {
            _budgetConstraint.Set(GRB.DoubleAttr.RHS, budget ?? GRB.INFINITY);
        }

        public void SetTimeslotsUsedConstraint(double? ts = null)
        {
            _vartotaltimeslotsused.Set(GRB.DoubleAttr.UB, ts ?? Data.TimeSlots.Count);
        }



        public void SetBadTimeslotUB(int ts)
        {
            _varBadTimeSlotsViol.Set(GRB.DoubleAttr.UB,ts);
        }
        
        public void RelaxModel()
        {
            foreach (var grbVar in varY)
            {
                grbVar.Value.Set(GRB.CharAttr.VType,GRB.CONTINUOUS);
            }
            foreach (var grbVar in varR)
            {
                grbVar.Value.Set(GRB.CharAttr.VType,GRB.CONTINUOUS);
            }
            if (varX != null)
            {
                foreach (var grbVar in varX)
                {
                    grbVar.Value.Set(GRB.CharAttr.VType, GRB.CONTINUOUS);
                }
            }

            foreach (var grbVar in _varRplus)
            {
                grbVar.Value.Set(GRB.CharAttr.VType,GRB.CONTINUOUS);
            }
            _model.Reset();
        }



        public void SetPossibleRoomCombinations(int budget,int? budget2 = null)
        {
            Console.WriteLine("notice that also works with 25 intervals");
            var roomcombinations = new RoomCombinations(Data,25);
            var combs = roomcombinations.GetCombinations(budget);

            Console.WriteLine($"Added {combs.Count} combinations");

            //Make binary var to choose combination.
            var vars = new List<GRBVar>();
            for (int i = 0; i < combs.Count; i++)
            {
                var newvar = _model.AddVar(0, 1, 0, GRB.BINARY, $"roomcomb_{budget}_{i}");
                vars.Add(newvar);
            }

            _model.Update();
            var sumofvars = new GRBLinExpr();
            for (int i = 0; i < combs.Count; i++)
            {
                sumofvars += vars[i];
            }

            List<Dictionary<int, int>> combs2 = null;
            List<GRBVar> vars2 = null;
            if (budget2 != null)
            {
                combs2 = roomcombinations.GetCombinations((int) budget2);

                Console.WriteLine($"Budget2: Added {combs2.Count} combinations");

                //Make binary var to choose combination.
                vars2 = new List<GRBVar>();
                for (int i = 0; i < combs2.Count; i++)
                {
                    vars2.Add(_model.AddVar(0, 1, 0, GRB.BINARY, $"roomcomb_{(int) budget2}_{i}"));
                }

                _model.Update();
                var sumofvars2 = new GRBLinExpr();
                for (int i = 0; i < combs2.Count; i++)
                {
                    sumofvars2 += vars2[i];
                }

                sumofvars += sumofvars2;
            //vars2.ForEach(v => v.Set(GRB.IntAttr.BranchPriority, 10));

            }
            //vars.ForEach(v => v.Set(GRB.IntAttr.BranchPriority, 10));


            _model.AddConstr(sumofvars, GRB.EQUAL, 1, $"chooseonecombination");


            foreach (var var in varR)
            {
                var rvars = new GRBLinExpr();
                for (int i = 0; i < combs.Count; i++)
                {
                    var capacity = var.Key.Capacity;
                    var numbs = combs[i].ContainsKey(capacity) ? combs[i][capacity] : 0;
                    rvars += numbs * vars[i];
                }
                if (budget2 != null)
                {
                    for (int i = 0; i < combs2.Count; i++)
                    {
                        var capacity = var.Key.Capacity;
                        var numbs = combs2[i].ContainsKey(capacity) ? combs2[i][capacity] : 0;
                        rvars += numbs * vars2[i];
                    }
                }
                _model.AddConstr(rvars, GRB.EQUAL, var.Value, $"roomcomb_{var.Key}");
            }
            // _budgetConstraint.Set(GRB.DoubleAttr.RHS, budget);

        }

        public void UnsetRoomasTypes()
        {
            
            foreach (var grbVar in varR)
            {
                grbVar.Value.Set(GRB.DoubleAttr.UB,1);
            }
        }

 

        public void SetQualConstraint(int? qual = null)
        {
            _qualityConstraint.Set(GRB.DoubleAttr.RHS, qual ?? GRB.INFINITY);
        }
        public void SetQualConstraintOnIndividualmeasssures(bool set = true)
        {
             _minimumworkingdaysbelow.Set(GRB.DoubleAttr.UB,set ? _minimumworkingdaysbelow.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            foreach (var dbvar in _varDaysBelow.Values)
            {
            //    dbvar.Set(GRB.DoubleAttr.UB, set ? dbvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            //    dbvar.Set(GRB.DoubleAttr.LB, set ? dbvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            }
            foreach (var dvar in _varDayinUse.Values)
            {
                dvar.Set(GRB.DoubleAttr.UB, set ? dvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
                dvar.Set(GRB.DoubleAttr.LB, set ? dvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            }

            _roomStability.Set(GRB.DoubleAttr.UB, set ? _roomStability.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
                _studentminmaxload.Set(GRB.DoubleAttr.UB, set ? _studentminmaxload.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
                _varCurrCompactViol.Set(GRB.DoubleAttr.UB, set ? _varCurrCompactViol.Get(GRB.DoubleAttr.X) : GRB.INFINITY);

            //Fixing all curricula positions.
            // This seems to strict
            foreach (var cvar in _varcurrAssigned.Values.SelectMany(kv => kv.Values))
            {
           //     cvar.Set(GRB.DoubleAttr.UB, set ? cvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            }
            foreach (var cvar in _varcurrAlone.Values.SelectMany(kv => kv.Values))
            {
            //    cvar.Set(GRB.DoubleAttr.LB, set ? cvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);
            //    cvar.Set(GRB.DoubleAttr.UB, set ? cvar.Get(GRB.DoubleAttr.X) : GRB.INFINITY);

            }
            //Fixing each curriculas value.


        }
        public void SetProxConstraint(int? k = null)
        {
            _localBranchingConstraint.Set(GRB.DoubleAttr.RHS, k ?? GRB.INFINITY);
        }

        public void SetProcConstraintSenseEqual(bool equalcnst = false)
        {
            if (equalcnst)
            {
                _localBranchingConstraint.Sense = GRB.EQUAL;
            }
            else
            {
                _localBranchingConstraint.Sense = GRB.LESS_EQUAL;
            }
        }

        public void SetObjective(double softconst, double cost,double proximity = 0,double badtimeslots = 0,double totaltimeslots = 0)
        {
            _softConstraints.Set(GRB.DoubleAttr.Obj, softconst);
            _cost.Set(GRB.DoubleAttr.Obj, cost);
            _proximity.Set(GRB.DoubleAttr.Obj, proximity);
            _varBadTimeSlotsViol.Set(GRB.DoubleAttr.Obj, badtimeslots);
            _vartotaltimeslotsused.Set(GRB.DoubleAttr.Obj, totaltimeslots);
        }

        public void DisplayObjectives()
        {
            Console.WriteLine(
                $"Objectives:\nSoftConstraints:{_softConstraints.Get(GRB.DoubleAttr.X) :####}\n"+
                $"MinimumWorkingDays: {_minimumworkingdaysbelow.Get(GRB.DoubleAttr.X):####}\n" +
                $"CurricuclumCompactness: {ObjCC:####}\n" +
                $"Proximity: { _proximity.Get(GRB.DoubleAttr.X):####}\n");
        }

        public void FixPenaltyToZero(bool fix)
        {
            var cpUB = ModelParameters.ConstraintPenalty == null ? 0 : GRB.INFINITY;

            ConsPenalty.Set(GRB.DoubleAttr.UB, fix ? 0 : cpUB);
        }


        public void PenalizeRoomUsedMoreThanOnce(int penalty,bool penalizeEachTimeslot = false)
        {
            if (penalizeEachTimeslot)
            {
                foreach (var kv in _roomOverUsePenalty)
                {
                    kv.Value.Set(GRB.DoubleAttr.Obj,penalty * ((double) kv.Key.Item1.Value/Data.TimeSlots.Count));
                }
                return;
            }

            foreach (var room in Data.Rooms)
            {
                varR[room].Set(GRB.DoubleAttr.LB,1);
                varR[room].Set(GRB.DoubleAttr.Obj, penalty);
           //     varR[room].Set(GRB.DoubleAttr.Obj, room.Cost);
            }
        }



        public void MinimizeRoomwithPen(int penalty,bool penalizeEachDay = false)
        {
            if (penalizeEachDay)
            {
                foreach (var value in _roomOverUsePenalty.Values)
                {
                    value.Set(GRB.DoubleAttr.Obj,penalty);
                }
          
            }

            foreach (var room in Data.Rooms)
            {
            //    varR[room].Set(GRB.DoubleAttr.LB,1);
            //    varR[room].Set(GRB.DoubleAttr.Obj, penalty);
                varR[room].Set(GRB.DoubleAttr.Obj, room.Cost);
            }
        }

        public void PenalizeUnavailability(int penalty)
        {
            foreach (var ct in varY.Keys)
            {
                if (ct.Item1.UnavailableTimeSlots.Contains(ct.Item2))
                {
                    varY[ct].Set(GRB.DoubleAttr.Obj,penalty);
                }
            }
        }

        public bool Optimize(double timelimit = GRB.INFINITY, double mipGap = 0.00, double? cutoff = null)
        {

            //  _model.Write(@"c:\temp\model.lp");
            _model.GetEnv().Set(GRB.IntParam.Seed, ModelParameters.Seed);

            _model.GetEnv().Set(GRB.DoubleParam.TimeLimit, timelimit);
            _model.GetEnv().Set(GRB.DoubleParam.MIPGap, mipGap);
            _model.GetEnv().Set(GRB.DoubleParam.Cutoff, cutoff ?? GRB.INFINITY);
            
            if (ModelParameters.TuneGurobi)
            {
                _model.GetEnv().Set(GRB.DoubleParam.Heuristics, 0.5);
                //  Console.WriteLine("\n\n!!No MIPFOCUS!!\n\n");
                _model.GetEnv().Set(GRB.IntParam.MIPFocus, 1);

            }

            if (ModelParameters.FocusOnlyOnBounds)
            {
                Console.WriteLine("Warning: Only focusing on bound");
                _model.GetEnv().Set(GRB.IntParam.MIPFocus, 3);
                _model.GetEnv().Set(GRB.DoubleParam.Heuristics, 0.0);
            }

            foreach (var rvar in _varRplus.Values)
            {
                rvar.Set(GRB.IntAttr.BranchPriority, 1);
            }
            if (ModelParameters.NSols > 1)
            {
                _model.GetEnv().Set(GRB.IntParam.SolutionLimit, ModelParameters.NSols);
            }


            _model.Optimize();



            if (_model.Get(GRB.IntAttr.Status) == GRB.Status.INF_OR_UNBD) return false;
            if (_model.Get(GRB.IntAttr.SolCount) < 1) return false;

            if (_model.Get(GRB.IntAttr.Status) == GRB.Status.INFEASIBLE)
            {
                return false;
                _model.ComputeIIS();
                Console.WriteLine("\nThe following constraints cannot be satisfied:");
                foreach (GRBConstr c in _model.GetConstrs())
                {
                    if (c.Get(GRB.IntAttr.IISConstr) == 1)
                    {
                        Console.WriteLine(c.Get(GRB.StringAttr.ConstrName));
                        // Remove a single constraint from the model

                    }
                }
                WriteModel(@"c:\temp\inf.ilp");
                return false;

            }
            return true;
        }

        public List<Assignment> GetAssignments()
        {
            if (varX == null)
            {
                return
                    (from key in varY.Keys
                        where varY[key].Get(GRB.DoubleAttr.X) > 0.5
                        select new Assignment(key.Item1, key.Item2)).ToList();
            }
            else
            {
                return
                    (from key in varX.Keys
                        where varX[key].Get(GRB.DoubleAttr.X) > 0.5
                        select new Assignment(key.Item1, key.Item2, key.Item3)).ToList();
            }
        }

        public Dictionary<Room,int> GetUsedRooms()
        {
            var rooms = new Dictionary<Room,int>();
            foreach (var room in Data.Rooms)
            {
                var val = (int) Math.Round(varR[room].Get(GRB.DoubleAttr.X));
               // if (val == 0)   continue;
              rooms.Add(room,val);
            }
            return rooms;
        }

        public double GetOverUseOfRooms()
        {
            return _roomOverUsePenalty.Sum(r => r.Value.Get(GRB.DoubleAttr.X));
        }


        public int GetUnavailableused()
        {
            return varY.Count(y => y.Key.Item1.UnavailableTimeSlots.Contains(y.Key.Item2) && y.Value.Get(GRB.DoubleAttr.X) > 0.5);
        }

        public List<Tuple<double, int, Dictionary<Room, int>, List<Assignment>>> GetAllSolutions()
        {
            var sols = new List<Tuple<double, int, Dictionary<Room, int>, List<Assignment>>>();

            for (int i = 0; i < _model.Get(GRB.IntAttr.SolCount); i++)
            {
                _model.GetEnv().Set(GRB.IntParam.SolutionNumber, i);

                var softcost = (int) Math.Round(_softConstraints.Get(GRB.DoubleAttr.Xn));
                var rcoost = _cost.Get(GRB.DoubleAttr.Xn);

                //Rooms
                var rooms = new Dictionary<Room, int>();
                foreach (var room in Data.Rooms)
                {
                    var val = (int)Math.Round(varR[room].Get(GRB.DoubleAttr.Xn));
                    // if (val == 0)   continue;
                    rooms.Add(room, val);
                }
                
                //assignments
                var assignments = new List<Assignment>();
                if (varX == null)
                {
                    assignments =
                        (from key in varY.Keys
                         where varY[key].Get(GRB.DoubleAttr.Xn) > 0.5
                         select new Assignment(key.Item1, key.Item2)).ToList();
                }
                else
                {
                    assignments =
                        (from key in varX.Keys
                         where varX[key].Get(GRB.DoubleAttr.Xn) > 0.5
                         select new Assignment(key.Item1, key.Item2, key.Item3)).ToList();
                }

                sols.Add(Tuple.Create(rcoost, softcost, rooms,assignments));

            }

            return sols;
        }

        public void DisplaySoftConstraintViolations()
        {
            foreach (var curriculum in Data.Curricula)
            {
                //if (curriculum.Courses.Sum(c => c.Lectures) != _studentLoad[curriculum].SelectMany(c => c.Value) Sum(x => x. .Get(GRB.DoubleAttr.X)))
                foreach (var day in Data.TimeSlotsByDay)
                {
                    if (true || _studentLoadViol[curriculum][day.Key].Get(GRB.DoubleAttr.X) > 0.5)
                    {
                        Console.WriteLine(
                            $"StudentLoad: {curriculum} {day.Key}: {_studentLoad[curriculum][day.Key].Get(GRB.DoubleAttr.X)}");
                        Console.WriteLine(
                            $"StudentLoadViol: {curriculum} {day.Key}: {_studentLoadViol[curriculum][day.Key].Get(GRB.DoubleAttr.X)}");
                    }
                }
            }
        }


  

        public void MipStart(List<Assignment> assignments)
        {
            foreach (var assignment in assignments)
            {
                if (assignment.Room != null && varX != null)
                {
                    varX[Tuple.Create(assignment.Course,assignment.TimeSlot,assignment.Room)].Set(GRB.DoubleAttr.Start,1);
                }
             //   varX[Tuple.Create(assignment.Course,assignment.TimeSlot,assignment.Room)].Set(GRB.DoubleAttr.X,1);                
                varY[Tuple.Create(assignment.Course,assignment.TimeSlot)].Set(GRB.DoubleAttr.Start,1);           
            //    varY[Tuple.Create(assignment.Course,assignment.TimeSlot)].Set(GRB.DoubleAttr.X,1);
            }
        }
        public void SetAndFixSolution(List<Assignment> assignments)
        {
            foreach (var assignment in assignments)
            {
                if (assignment.Room == null || ModelParameters.UseStageIandII == false)
                    varY[Tuple.Create(assignment.Course, assignment.TimeSlot)].Set(GRB.DoubleAttr.LB, 1);

                else
                    varX[Tuple.Create(assignment.Course,assignment.TimeSlot,assignment.Room)].Set(GRB.DoubleAttr.LB,1);
            }
        }
        public void Fixsol(bool doFix)
        {
            foreach (var assignment in varY.Keys)
            {
                if (varY[assignment].Get(GRB.DoubleAttr.X) < 0.5) continue;
                varY[assignment].Set(GRB.DoubleAttr.LB, doFix ? 1 : 0);
            }

            if (ModelParameters.UseStageIandII)
            {
                foreach (var assignment in varX.Keys)
                {
                    if (varX[assignment].Get(GRB.DoubleAttr.X) < 0.5) continue;
                    varX[assignment].Set(GRB.DoubleAttr.LB, doFix ? 1 : 0);
                }
            }

        }

        public void FixsolRooms(bool doFix)
        {
            foreach (var r in _varRplus.Keys)
            {
                var value =_varRplus[r].Get(GRB.DoubleAttr.X);
                _varRplus[r].Set(GRB.DoubleAttr.LB, doFix ? value : 0);
                _varRplus[r].Set(GRB.DoubleAttr.UB, doFix ? value : GRB.INFINITY);
            }
        }

        public void FixCurricula(bool doFix)
        {
            foreach (var curriculum in Data.Curricula)
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    if (_varcurrAssigned[curriculum][timeSlot].Get(GRB.DoubleAttr.X) < 0.5) continue;

                    _varcurrAssigned[curriculum][timeSlot].Set(GRB.DoubleAttr.LB,doFix ? 1 : 0);
                }
            }
        }

        public void AddMinimumWorkingDaysCost()
        {
            _minimumworkingdaysbelow.Set(GRB.DoubleAttr.Obj, ProblemFormulation.MinimumWorkingDaysWeight);
        }

        public void SetProximityOrigin(bool useCurrPlanned = false)
        {
            if (_proxCalc != null) _model.Remove(_proxCalc);

            if (!useCurrPlanned)
            {
                _proxCalc =
                    _model.AddConstr(
                        varY.Count(v => v.Value.Get(GRB.DoubleAttr.X) > 0.5) -
                        varY.Where(v => v.Value.Get(GRB.DoubleAttr.X) > 0.5).Sumx(v => v.Value),
                        GRB.EQUAL, _proximity, "proxcalc");
            }
            else
            {
                
            var currassignedvars = _varcurrAssigned.Values.SelectMany(v => v.Values);

            _proxCalc = _model.AddConstr(currassignedvars.Count(v => v.Get(GRB.DoubleAttr.X) > 0.5) - currassignedvars.Where(v => v.Get(GRB.DoubleAttr.X) > 0.5).Sumx(v => v),
                GRB.EQUAL, _proximity, "proxcalc");
            }
        }

        public void SetProximityOrigin(List<Assignment> assignments)
        {
            if (_proxCalc != null) _model.Remove(_proxCalc);

            _proxCalc =
                   _model.AddConstr(
                       varX.Count(v => assignments.Exists(a => a.Course.Equals(v.Key.Item1) && 
                       a.TimeSlot.Equals(v.Key.Item2) && 
                       a.Room.Equals(v.Key.Item3))) -
                       varX.Where(v => assignments.Exists(a => a.Course.Equals(v.Key.Item1) && 
                       a.TimeSlot.Equals(v.Key.Item2) && 
                       a.Room.Equals(v.Key.Item3))).Sumx(v => v.Value),
                       GRB.EQUAL, _proximity, "proxcalc");
        }

        public void RemoveCurrentSolution()
        {
            _model.AddConstr(
                     varX.Count(v => v.Value.Get(GRB.DoubleAttr.X) > 0.5) -
                     varX.Where(v => v.Value.Get(GRB.DoubleAttr.X) > 0.5).Sumx(v => v.Value),
                     GRB.GREATER_EQUAL, 1, "solutionremove");
        }

        public void SetMipHintToCurrent(bool useCurrPlanned = false)
        {
       
            if (!useCurrPlanned)
            {
                foreach (var @var in varY)
                {
                    @var.Value.Set(GRB.DoubleAttr.VarHintVal, @var.Value.Get(GRB.DoubleAttr.X));
                }
            }
            else
            {
                var currassignedvars = _varcurrAssigned.Values.SelectMany(v => v.Values);

                foreach (var @var in currassignedvars)
                {
                    @var.Set(GRB.DoubleAttr.VarHintVal, @var.Get(GRB.DoubleAttr.X));
                }

            }
        }

        public void SetMipHints(List<Tuple<Course, TimeSlot, int>> hints, bool fixhints = false)
        {
            foreach (var hint in hints)
            {
                if (fixhints) varY[Tuple.Create(hint.Item1, hint.Item2)].Set(GRB.DoubleAttr.LB, 1);
                else
                {
                    varY[Tuple.Create(hint.Item1, hint.Item2)].Set(GRB.DoubleAttr.VarHintVal, 1);
                    varY[Tuple.Create(hint.Item1, hint.Item2)].Set(GRB.IntAttr.VarHintPri, hint.Item3);
                }

            }
        }

        public void SetMipHints(List<Tuple<Curriculum, TimeSlot, int>> hints)
        {
            foreach (var hint in hints)
            {
             //   if (hint.Item3 > 9) _varcurrAssigned[hint.Item1][hint.Item2].Set(GRB.DoubleAttr.LB, 1);
                _varcurrAssigned[hint.Item1][hint.Item2].Set(GRB.DoubleAttr.VarHintVal,1);
                _varcurrAssigned[hint.Item1][hint.Item2].Set(GRB.IntAttr.VarHintPri, hint.Item3);
            }
        }
    public void SetMipHints(List<Assignment> assignments)
        {
            foreach (var ass in assignments)
            {
               varY[Tuple.Create(ass.Course,ass.TimeSlot)].Set(GRB.DoubleAttr.VarHintVal, 1);
               varX[Tuple.Create(ass.Course,ass.TimeSlot,ass.Room)].Set(GRB.DoubleAttr.VarHintVal, 1);

            }
        }


        public void WriteModel(string filename)
        {
            _model.Update();
            _model.Write(filename);
        }


        public void Disrupt(Room room)
        {
            foreach (var x in varX.Where(x => x.Key.Item3.Equals(room)))
            {
                x.Value.Set(GRB.DoubleAttr.UB,0);
            }
        }
        public void Disrupt(TimeSlot timeSlot)
        {
            foreach (var x in varX.Where(x => x.Key.Item2.Equals(timeSlot)))
            {
                x.Value.Set(GRB.DoubleAttr.UB,0);
            }
        }
        public void Disrupt(Course course, TimeSlot timeSlot)
        {
            foreach (var x in varX.Where(x => x.Key.Item1.Equals(course) && x.Key.Item2.Equals(timeSlot)))
            {
                x.Value.Set(GRB.DoubleAttr.UB, 0);
            }
        }
        public void Disrupt(Room room, List<TimeSlot> timeSlots)
        {
            foreach (var x in varX.Where(x => x.Key.Item3.Equals(room) && timeSlots.Contains(x.Key.Item2)))
            {
                x.Value.Set(GRB.DoubleAttr.UB, 0);
            }
        }

        public void Disrupt(Curriculum curr,bool includeobj = false)
        {
            if (includeobj)
            {
                _varcurrAlone[curr] = new Dictionary<TimeSlot, GRBVar>();
                _varcurrAssigned[curr] = new Dictionary<TimeSlot, GRBVar>();
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _varcurrAlone[curr][timeSlot] = _model.AddVar(0, 1, ProblemFormulation.CurriculumCompactnessWeight,
                        GRB.CONTINUOUS, $"alone_{curr}_{timeSlot}");
                    _varcurrAssigned[curr][timeSlot] = _model.AddVar(0, 1, 0,
                        GRB.CONTINUOUS, $"assigned_{curr}_{timeSlot}");
                }

                var varyByTimeslot = varY.GroupBy(y => y.Key.Item2).ToDictionary(y => y.Key);

                foreach (var timeSlot in Data.TimeSlots)
                {
                    _model.AddConstr(_varcurrAssigned[curr][timeSlot], GRB.EQUAL,
                        varyByTimeslot[timeSlot].Where(t => curr.Courses.Contains(t.Key.Item1))
                            .Select(d => d.Value)
                            .Sumx(), $"currAss_{curr}_{timeSlot}");

                    _model.AddConstr(_varcurrAlone[curr][timeSlot], GRB.GREATER_EQUAL,
                        _varcurrAssigned[curr][timeSlot] -
                        _varcurrAssigned[curr].Where(d => TimeSlot.TimeSlotsAreConsequtive(d.Key, timeSlot))
                            .Select(d => d.Value)
                            .Sumx(), $"currAlone_{curr}_{timeSlot}");
                }
            }
            else
            {
                foreach (var timeSlot in Data.TimeSlots)
                {
                    _model.AddConstr(curr.Courses.Select(c => 1*varY[Tuple.Create(c, timeSlot)]).Sumx(), GRB.LESS_EQUAL,
                        1,
                        $"Curr_{curr}_{timeSlot}");
                }
            }
        }
    }


    internal class CCTCallback : GRBCallback
    {
        private CCTModel _model;
        public CCTCallback(CCTModel model)
        {
            _model = model;
        }


        protected override void Callback()
        {
            if (where == GRB.Callback.MESSAGE)
            {
                Console.Write(GetStringInfo(GRB.Callback.MSG_STRING));
                Console.Out.Flush();

            }

            if (where == GRB.Callback.MIPSOL)
            {
                var conspenaltyzero = Equals(_model.ConsPenalty, null) || (GetSolution(_model.ConsPenalty) < 0.1);
                var roomoveruserzero = Equals(_model._roomOverUsePenalty, null) || _model._roomOverUsePenalty.Values.All(v => GetSolution(v) < 0.1);

                if (_model.ModelParameters.AbortSolverOnZeroPenalty && 
                    conspenaltyzero && roomoveruserzero)
                {
                    Abort(); 
                }
            }
            if (where == GRB.Callback.MIPNODE)
            {
                if (_model.ModelParameters.SaveRelaxations)
                {
                    throw new NotImplementedException("this is not made");
                 //   _model.Relaxations.Add(Tuple.Create(GetNodeRel(_model.)));
                } 
            }

            if (where == GRB.Callback.MIPNODE &&
                _model.ModelParameters.SaveBoundInformation &&
                GetIntInfo(GRB.Callback.MIPNODE_STATUS) == GRB.Status.OPTIMAL)
            {
                try
                {
                    var mwd = 5*GetNodeRel(_model._minimumworkingdaysbelow);
                    var cc = GetNodeRel(_model._softConstraints) - mwd;
                    var obj = GetDoubleInfo(GRB.Callback.MIPNODE_OBJBST);
                    var bnd = GetDoubleInfo(GRB.Callback.MIPNODE_OBJBND);
                    var cost = GetNodeRel(_model._cost);
                    var time = GetDoubleInfo(GRB.Callback.RUNTIME);
                    _model.bounds.Enqueue(Tuple.Create(mwd,cc,obj,bnd,cost,time));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    internal class OutputCallback : GRBCallback
    {
     

        protected override void Callback()
        {
            if (where == GRB.Callback.MESSAGE)
            {
                Console.Write(GetStringInfo(GRB.Callback.MSG_STRING));
            }
            
        }
    }
}