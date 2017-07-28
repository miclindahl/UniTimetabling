using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniversityTimetabling.MIPModels;

namespace UniversityTimetabling.StrategicOpt
{
    public class MultiObjectiveSolver
    {
        private Data _data;
        public int Timelimit { get; set; }
        public double MIPGap { get; set; } = 0.00;
        public int Steps { get; set; } = 10;
        public ProblemFormulation Formulation; 
        public CCTModel.MIPModelParameters MipParameters;
        private CCTModel _model;
        private Stopwatch _stopwatch;
        private List<MultiResult> _multiResults;
        public bool EpsilonRelaxing { get; set; } = false;
        public int LocalBranching { get; set; } = 0;
        public bool UseMipHintToGuide { get; set; } = false;
        public bool EpsilonOnQuality { get; set; } = true;
        public bool UseTimeslotNotRoom { get; set; } = false;
        public List<Assignment> FixedTimeSlotAssignments { get; set; } = null;

        public int ExtraTimeOnCornerPointsMultiplier { get; set; } = 4;
        public bool UseSpecificObjConstraints { get; set; } = false;
        public bool DoubleSweep { get; set; } = false;
        public bool UseEpsilonMethod { get; set; } = true;

        public class MultiResult
        {
            public double Cost { get; }
            public int SoftObjective { get; }
            public int? CostBound { get; set; }
            public int? SoftObjBound { get; set; }

            public double? CostConstraint { get; }
            public double? QualConstraint { get; }

            public int? SecondsSinceStart { get; }

            public List<Assignment> Assignments { get; }
            public Dictionary<Room, int> UsedRooms { get; }


            public MultiResult(double cost, int softObjective, int? costBound = null, int? softobjBound = null,
                Dictionary<Room, int> usedRooms = null, List<Assignment> assignments = null, double? costConstraint = null, double? qualConstraint = null, int? secondsSinceStart = null)
            {
                Cost = cost;
                SoftObjective = softObjective;
                CostBound = costBound;
                SoftObjBound = softobjBound;
                UsedRooms = usedRooms;
                Assignments = assignments;
                CostConstraint = costConstraint;
                QualConstraint = qualConstraint;
                SecondsSinceStart = secondsSinceStart;
            }

        }

        public MultiObjectiveSolver(Data data,ProblemFormulation formulation,CCTModel.MIPModelParameters mipparameters)
        {
            Formulation = formulation;
            _data = data;
            MipParameters = mipparameters;
            mipparameters.NSols = 100;
            _model = new CCTModel(_data, Formulation, MipParameters);
        }

        private void StartLogging()
        {
           if(_stopwatch == null)  _stopwatch = Stopwatch.StartNew();
           if (_multiResults == null)  _multiResults = new List<MultiResult>();
        }


        public List<MultiResult> Run()
        {
            StartLogging();
           
            if (FixedTimeSlotAssignments != null)
            {
                Console.WriteLine("Fixing timeslots");
                _model.SetAndFixSolution(FixedTimeSlotAssignments);
            }
                
    
            double minCost;
            int maxSoftConsViol;
            int minSoftConsViol;
            double maxCost;
            int costBound;
            int softObjBound;
            CalculateCornerPoints(_multiResults, out minCost, out maxCost, out maxSoftConsViol, out minSoftConsViol, out costBound, out softObjBound);

            Console.WriteLine($"Slope between corners: {(maxCost-minCost)/(maxSoftConsViol-minSoftConsViol) : #.##}\nPenalty: {_model.ModelParameters.ConstraintPenalty}");

            var pareto = GetParetoPoints(_multiResults);
            
            if (Math.Abs(minCost - maxCost) < 0.05)
            {
                Console.WriteLine("Only one solution found. Returning");
                return pareto;
            }

            if (UseEpsilonMethod) RunEpsilonMethod(minSoftConsViol, maxSoftConsViol, minCost, maxCost);
            else RunWieghtedSumMetod(minSoftConsViol, maxSoftConsViol, minCost, maxCost);

            _stopwatch.Stop();

            //todo: refactor this in to its own function.
            Console.WriteLine($"Elapsed time: {_stopwatch.Elapsed}  -- {_stopwatch.ElapsedMilliseconds}ms");
            
            pareto = GetParetoPoints(_multiResults);
            Console.WriteLine($"\nBounds:\nCost: {costBound}\nSoftObj: {softObjBound}\n");
            DisplaySolutions(pareto);


            DisplayRoomProfiles(pareto);
            DisplayTimeSlots(pareto);
            DisplayChanges(pareto);
            return pareto;
        }

        private void RunEpsilonMethod(int minSoftConsViol, int maxSoftConsViol, double minCost, double maxCost)
        {
            
            List<MultiResult> pareto;
            var stepsize = (double) 1/(Steps); //(double)1 / (Steps + 1);
            var proxobj = 0;
            if (EpsilonOnQuality)
            {
                _model.SetObjective(0, 1, proxobj);
            }
            else
            {
                _model.SetObjective(1, 0, proxobj);
            }

            if (LocalBranching > 0) _model.SetProxConstraint(LocalBranching);

            for (var alpha = EpsilonRelaxing ? stepsize : 1 - stepsize;
                alpha >= 0 && alpha <= 1;
                //maybe we maybe should solve the last one again.. this seems to give improvementts.
                alpha += (EpsilonRelaxing ? 1 : -1)*stepsize)
            {
                Console.WriteLine($"Elapsed time: {_stopwatch.Elapsed}  -- {_stopwatch.ElapsedMilliseconds}ms");
                OptimizeSubProblem(minSoftConsViol, maxSoftConsViol, minCost, maxCost, alpha);
            }

            if (DoubleSweep)
            {
                pareto = GetParetoPoints(_multiResults);
                var beforepoints = pareto.Count;
                DisplaySolutions(pareto);

                //Check if new corner points
                minCost = pareto.Min(r => r.Cost);
                maxCost = pareto.Max(r => r.Cost);
                minSoftConsViol = pareto.Min(r => r.SoftObjective);
                maxSoftConsViol = pareto.Max(r => r.SoftObjective);

                Console.WriteLine("Running opposite direction");
                if (LocalBranching > 0) _model.SetProxConstraint(LocalBranching);

                for (var alpha = !EpsilonRelaxing ? stepsize : 1 - stepsize;
                    alpha > 0 && alpha < 1;
                    //maybe we maybe should solve the last one again.. this seems to give improvementts.
                    alpha += (!EpsilonRelaxing ? 1 : -1)*stepsize)
                {
                    Console.WriteLine($"Elapsed time: {_stopwatch.Elapsed}  -- {_stopwatch.ElapsedMilliseconds}ms");
                    OptimizeSubProblem(minSoftConsViol, maxSoftConsViol, minCost, maxCost, alpha);
                }

                pareto = GetParetoPoints(_multiResults);
                Console.WriteLine($"Done with oposite. New points added: {pareto.Count - beforepoints}");
            }
        }

        private void RunWieghtedSumMetod(int minSoftConsViol, int maxSoftConsViol, double minCost, double maxCost)
        {
            List<MultiResult> pareto;

            var queue = new Queue<Tuple<int,int,int,int>>();
            queue.Enqueue(Tuple.Create(minSoftConsViol, maxSoftConsViol,(int) minCost,(int) maxCost));
            for (int i = 0; i < Steps; i++)
            {
                if (queue.Count == 0)
                {
                    Console.WriteLine("Queue empty.. breaking");
                    break;
                }
                var point = queue.Dequeue();
                Console.WriteLine($"Point: {point}");

                var softconstweight = point.Item4-point.Item3;
                var costweight = point.Item2 - point.Item1;

                if (costweight < 0.1 || softconstweight < 0.1)
                {
                    Console.WriteLine("no slope.. skipping");
                    continue;
                }

                _model.SetObjective(softconstweight, costweight);

                Console.WriteLine($"Slope set to: {(double) softconstweight/costweight : #.##}");
                var solfound =_model.Optimize(Timelimit, MIPGap);
                Console.WriteLine($"LastSolution:\n(cost,softobj) = {_model.ObjCost} , {_model.ObjSoftCons}");
                if (solfound)
                {
                    queue.Enqueue(Tuple.Create(point.Item1, _model.ObjSoftCons,(int) _model.ObjCost, point.Item4));
                    queue.Enqueue(Tuple.Create(_model.ObjSoftCons, point.Item2, point.Item3, (int)_model.ObjCost));
                    // Maybe remove dominated solutions.
                    // _model.ObjBound <= cost*costweight + softobj*softobjweigh
                    var costBound = (int) Math.Floor(_model.ObjBound/costweight);
                    var softobjBound = (int) Math.Floor(_model.ObjBound / softconstweight);
                   
                    _multiResults.Add(new MultiResult(_model.ObjCost, _model.ObjSoftCons, costBound, softobjBound,
                        _model.GetUsedRooms(),
                        _model.GetAssignments(), null, null, (int) _stopwatch.Elapsed.TotalSeconds));

                }
                  AddAllSolutions(_model);
            }

        }

        public List<MultiResult> RunExhaustiveMethod()
        {
            if (EpsilonOnQuality) throw new ArgumentException("This only works on cost objective"); 
            StartLogging();
            
            if (FixedTimeSlotAssignments != null)
            {
                Console.WriteLine("Fixing timeslots");
                _model.SetAndFixSolution(FixedTimeSlotAssignments);
            }

            // calc far out corner pint
            double maxcost;
            int minobj;
            int minobjbounds;
            CalcLexMaxCostMinSoftcons(out maxcost,out minobj,out minobjbounds);
            _model.SetObjective(1,0);
      
            
            var seatConstraint = 25;
            while(seatConstraint < maxcost)
            {

                Console.WriteLine($"SeatConstraint: {seatConstraint}");
                _model.SetBudgetConstraint(seatConstraint);

                var sol = _model.Optimize(Timelimit);
                if (sol)
                {
                    _multiResults.Add(new MultiResult(_model.ObjCost, _model.ObjSoftCons, null, (int)Math.Ceiling(_model.ObjBound), _model.GetUsedRooms(), _model.GetAssignments(),seatConstraint, null, (int)_stopwatch.Elapsed.TotalSeconds));

                    if ((int)Math.Round(_model.Objective) <= minobj) break;
                }
                //  else solutions.Add(Tuple.Create(nincluded,-1));
                seatConstraint += 25;
            }

            _stopwatch.Stop();

           
            Console.WriteLine($"Elapsed time: {_stopwatch.Elapsed}  -- {_stopwatch.ElapsedMilliseconds}ms");

            var pareto = GetParetoPoints(_multiResults);
           // Console.WriteLine($"\nBounds:\nCost: {costBound}\nSoftObj: {softObjBound}\n");
            DisplaySolutions(pareto);


            DisplayRoomProfiles(pareto);
            DisplayTimeSlots(pareto);
            DisplayChanges(pareto);

            return pareto;
        }

        private void OptimizeSubProblem(int minSoftConsViol, int maxSoftConsViol, double minCost, double maxCost, double alpha)
        {
            StartLogging();

            double? qual = null;
            double? budget = null;
            if (EpsilonOnQuality)
            {
                qual = Math.Floor(minSoftConsViol*(1 - alpha) + maxSoftConsViol*alpha);
                _model.SetQualConstraint((int?) Math.Floor((double) qual));
                Console.WriteLine($"Alpha: {alpha:0.00} QualConstraint: {qual}");
            }
            else
            {
                budget = minCost*(1 - alpha) + maxCost*alpha;
                _model.SetBudgetConstraint(budget);
                Console.WriteLine($"Alpha: {alpha:0.00} BudgetConstraint: {budget}");
            }
            /*
               a weighting scheme
                model.SetObjective(alpha* minRoomCost, (1-alpha)* minQual);
                */
            if (LocalBranching > 0) _model.SetProximityOrigin();

            _model.Optimize(Timelimit, MIPGap);
            Console.WriteLine($"LastSolution:\n(cost,softobj) = {_model.ObjCost} , {_model.ObjSoftCons}");

            int? costBound = EpsilonOnQuality ?(int?) Math.Ceiling(_model.ObjBound) : null;
            int? softobjBound = !EpsilonOnQuality ? (int?) Math.Ceiling(_model.ObjBound) : null;

            _multiResults.Add(new MultiResult(_model.ObjCost, _model.ObjSoftCons, costBound, softobjBound, _model.GetUsedRooms(),
                _model.GetAssignments(), budget, qual, (int) _stopwatch.Elapsed.TotalSeconds));
            AddAllSolutions(_model);
        }

        private void AddAllSolutions(CCTModel model)
        {
            _multiResults.AddRange(model.GetAllSolutions().Select(r => new MultiResult(r.Item1, r.Item2, null, null, r.Item3,r.Item4,secondsSinceStart:(int) _stopwatch.Elapsed.TotalSeconds)));
        }

        private void CalculateCornerPoints(List<MultiResult> result, out double minCost, out double maxCost, out int maxSoftConsViol,
            out int minSoftConsViol,
            out int CostBound,
            out int SoftobjBound)
        {

            _model.FixPenaltyToZero(true);
            if (EpsilonRelaxing == EpsilonOnQuality)
            {
                CalcLexMinCostMaxSoft(out minCost, out maxSoftConsViol,out CostBound);

                CalcLexMaxCostMinSoftcons(out maxCost, out minSoftConsViol, out SoftobjBound);
            }
            else //epsilontightening
            {
                CalcLexMaxCostMinSoftcons(out maxCost, out minSoftConsViol, out SoftobjBound);

                CalcLexMinCostMaxSoft(out minCost, out maxSoftConsViol, out CostBound);
              
            }

            Console.WriteLine($"\nBounds:\nCost: {CostBound}\nSoftObj: {SoftobjBound}\n");

            Console.WriteLine($"Lexicographical solutions:");
            var pareto = GetParetoPoints(result);
            DisplaySolutions(pareto);
            DisplayRoomProfiles(pareto);
            DisplayTimeSlots(pareto);

            DisplayChanges(pareto);
            _model.FixPenaltyToZero(false);
        }

        public void CalcLexMaxCostMinSoftcons(out double maxCost, out int minSoftConsViol, out int minsoftconsBound)
        {
            StartLogging();

            Console.WriteLine("Finding Lexicogrpahic: Min Soft cost -> Min Cost");
            _model.SetObjective(1, 0);
            _model.Optimize(Timelimit* ExtraTimeOnCornerPointsMultiplier, MIPGap);
            minSoftConsViol = _model.ObjSoftCons;
            minsoftconsBound = (int) Math.Ceiling(_model.ObjBound);
            if (UseSpecificObjConstraints) _model.SetQualConstraintOnIndividualmeasssures();
            else _model.SetQualConstraint(minSoftConsViol);

            _model.SetObjective(0, 1);
            _model.Optimize(Timelimit* ExtraTimeOnCornerPointsMultiplier, MIPGap);
            maxCost = _model.ObjCost;
            Console.WriteLine($"Objective from model: {_model.Objective}");
            _multiResults.Add(
                new MultiResult(_model.ObjCost, _model.ObjSoftCons, (int)Math.Ceiling(_model.ObjBound), minsoftconsBound, _model.GetUsedRooms(), _model.GetAssignments(),null,minSoftConsViol, (int)_stopwatch.Elapsed.TotalSeconds)
                );
            AddAllSolutions(_model);

            //           sol.SetAssignments(model.GetAssignments());
            //           Console.WriteLine(sol.AnalyzeSolution());

            if (UseSpecificObjConstraints) _model.SetQualConstraintOnIndividualmeasssures(false);
            else _model.SetQualConstraint();


        }

        public void CalcLexMinCostMaxSoft(out double minCost, out int maxSoftConsViol, out int minCostBound)
        {
            StartLogging();
            Console.WriteLine("Finding Lexicogrpahic:  Min Cost -> Min Soft cost");

            _model.SetObjective(0, 1);
            _model.Optimize(Timelimit* ExtraTimeOnCornerPointsMultiplier, MIPGap);

            minCost = _model.Objective;
            minCostBound = (int) Math.Ceiling(_model.ObjBound);

            if (UseSpecificObjConstraints)  _model.FixsolRooms(true);

            _model.SetBudgetConstraint(minCost);
            
            _model.SetObjective(1, 0);
            _model.Optimize(Timelimit* ExtraTimeOnCornerPointsMultiplier, MIPGap);
            //            sol.SetAssignments(model.GetAssignments());
            //            Console.WriteLine(sol.AnalyzeSolution());

            maxSoftConsViol = _model.ObjSoftCons;
            _multiResults.Add(new MultiResult(_model.ObjCost, _model.ObjSoftCons, minCostBound, (int)Math.Ceiling(_model.ObjBound), _model.GetUsedRooms(), _model.GetAssignments(),minCost,null, (int)_stopwatch.Elapsed.TotalSeconds));

            AddAllSolutions(_model);
            _model.SetBudgetConstraint();

            //  model.FixsolRooms(false);
        }

        private List<MultiResult> GetParetoPoints(List<MultiResult> result)
        {
            var pareto = new List<MultiResult>();

            var domqual = int.MaxValue;
            foreach (var res in result.OrderBy(r => r.Cost).ThenBy(r => r.SoftObjective).ThenByDescending(r => r.CostBound))
            {
                if (res.SoftObjective >= domqual) continue;
                if (res.SoftObjective < domqual) domqual = res.SoftObjective;

                //Tighten bound
                if (EpsilonOnQuality)
                    res.CostBound = result.Where(r => r.QualConstraint >= res.QualConstraint || r.QualConstraint == null).Max(r => r.CostBound);
                else
                    res.SoftObjBound = result.Where(r => r.CostConstraint >= res.CostConstraint || r.CostConstraint == null).Max(r => r.SoftObjBound);

                pareto.Add(res);
            }

            return pareto;
        } 

        private void DisplaySolutions(List<MultiResult> result)
        {
            Console.WriteLine("cost;SoftObj;CostBound;SoftObjBound;CostConst;QualConst;Seconds");
            foreach (var res in result)
            {
                Console.WriteLine(
                    $"{res.Cost:##.#};{res.SoftObjective};{res.CostBound ?? double.NaN};{res.SoftObjBound ?? double.NaN};{res.CostConstraint ?? double.NaN};{res.QualConstraint ?? double.NaN};{res.SecondsSinceStart ?? double.NaN}");
            }
        }

        private void DisplayRoomProfiles(List<MultiResult> result)
        {
            Console.WriteLine("\nRoom profiles:");
            var usedRoomsMinimum = result.First(r => r.UsedRooms != null).UsedRooms;
            Console.WriteLine($"cost;#rooms;{string.Join(";", usedRoomsMinimum.Select(r => r.Key))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                Console.WriteLine(
                    $"{res.Cost:##.#};{res.UsedRooms.Sum(r => r.Value)};{string.Join(";",res.UsedRooms.Select(r => r.Value))}");
            }
            Console.WriteLine("\nAccumulated Room profiles:");
            Console.WriteLine($"{string.Join(";", usedRoomsMinimum.GroupBy(r => r.Key.Capacity).Select(r => "R" + r.Key))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                Console.WriteLine(
                    $"{string.Join(";", res.UsedRooms.GroupBy(r => r.Key.Capacity).Select(r => res.UsedRooms.Where(kv => kv.Key.Capacity >= r.Key).Sum(kv => kv.Value)))}");
            }
            Console.WriteLine("\nAccumulated Room profiles (Cost):");
            Console.WriteLine($"{string.Join(";", usedRoomsMinimum.GroupBy(r => r.Key.Capacity).Select(r => "R" + r.Key))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                Console.WriteLine(
                    $"{string.Join(";", res.UsedRooms.GroupBy(r => r.Key.Capacity).Select(r => res.UsedRooms.Where(kv => kv.Key.Capacity >= r.Key).Sum(kv => kv.Value*kv.Key.Cost)))}");
            }

            var minimumRoom =
                usedRoomsMinimum.GroupBy(r => r.Key.Capacity).ToDictionary(r => r.Key,r => usedRoomsMinimum.Where(kv => kv.Key.Capacity >= r.Key).Sum(kv => kv.Value));
            
            Console.WriteLine("\nDiff Room profiles:");

            Console.WriteLine($"{string.Join(";", usedRoomsMinimum.GroupBy(r => r.Key.Capacity).Select(r => "R" + r.Key))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                Console.WriteLine(
                    $"{string.Join(";", res.UsedRooms.GroupBy(r => r.Key.Capacity).Select(r => res.UsedRooms.Where(kv => kv.Key.Capacity >= r.Key).Sum(kv => kv.Value) - minimumRoom[r.Key]))}");
            }


        }

        private void DisplayTimeSlots(List<MultiResult> result)
        {
            //how many timeslots are availbable of size n or bigger.
            //Maybe look at timeslot with minimum timeslots available.
            var usedRoomsMinimum = result.First(r => r.UsedRooms != null).UsedRooms;

            var nsizes = result.First(r => r.UsedRooms != null).UsedRooms.Select(r => r.Key.Capacity).Distinct().ToList();
            nsizes.Add(0);
            nsizes.Sort();


            Console.WriteLine("\nAccumulated Course Sizes (>n)");
            Console.WriteLine($"{string.Join(";",nsizes.Select(r => "n" + r))}");
            var coursesnbigger = nsizes.ToDictionary(n => n,n=> result.First().Assignments.Count(a => a.Course.NumberOfStudents > n));
            Console.WriteLine($"{string.Join(";", coursesnbigger.Values)}");


            Console.WriteLine("\nAccumulated Timeslots Free (>n):");
            Console.WriteLine($"{string.Join(";", nsizes.Select(r => "n" + r))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                var availRoomsHours = nsizes.ToDictionary(n => n, n => res.UsedRooms.Where(rkv => rkv.Key.Capacity > n).Sum(rt => rt.Value*_data.TimeSlots.Count));
                var freeTimeslots = nsizes.ToDictionary(n => n,
                    n => availRoomsHours[n] - res.Assignments.Count(a => a.Course.NumberOfStudents >n));
                Console.WriteLine(
                    $"{string.Join(";", freeTimeslots.Select(r => r.Value))}");
            }



         Console.WriteLine("\nAccumulated Timeslots Free / #lecture (>n):");
                    Console.WriteLine($"{string.Join(";", nsizes.Select(r => "n" + r))}");

                    foreach (var res in result.Where(r => r.UsedRooms != null))
                    {
                        var availRoomsHours = nsizes.ToDictionary(n => n, n => res.UsedRooms.Where(rkv => rkv.Key.Capacity > n).Sum(rt => rt.Value*_data.TimeSlots.Count));
                        var freeTimeslots = nsizes.ToDictionary(n => n,
                            n => availRoomsHours[n] - res.Assignments.Count(a => a.Course.NumberOfStudents >n));
                        Console.WriteLine(
                            $"{string.Join(";", freeTimeslots.Select(r => $"{r.Value/(coursesnbigger[r.Key] == 0 ? 0.001 : coursesnbigger[r.Key]):0.00}")) }");
                    }


            Console.WriteLine("\nMinimum Room Free (>n):");
            Console.WriteLine($"{string.Join(";", nsizes.Select(r => "n" + r))}");

            foreach (var res in result.Where(r => r.UsedRooms != null))
            {
                var availRoomsHours = nsizes.ToDictionary(n => n, n => res.UsedRooms.Where(rkv => rkv.Key.Capacity > n).Sum(rt => rt.Value));
                var freeTimeslots = nsizes.ToDictionary(n => n,
                    n => res.Assignments.GroupBy(a => a.TimeSlot).Count(a => availRoomsHours[n] > 0 && 0 == availRoomsHours[n] - a.Count(at =>  at.Course.NumberOfStudents >n)));
                Console.WriteLine(
                    $"{string.Join(";", freeTimeslots.Select(r => r.Value))}");
            }
            //? Maybe percentage where minimum (=0?) free
            Console.WriteLine("\nPenalty assoiacted");
            Console.WriteLine($"{string.Join(";", nsizes.Select(r => "n" + r))}");

            foreach (var res in result)
            {
                
                var sol = new Solution(_data,Formulation);
                sol.SetAssignments(res.Assignments);
                sol.DoStageIRoomCheck = false;
                sol.AnalyzeSolution();

                var penalties = nsizes.ToDictionary(n => n,
                   n => sol.PenaltyForCourse.Where(kv => kv.Key.NumberOfStudents > n && kv.Key.NumberOfStudents <= n+25).Sum(kv => kv.Value));
                Console.WriteLine(
                   $"{string.Join(";", penalties.Select(r => r.Value))}");
            }
        }


        private void DisplayChanges(List<MultiResult> result)
        {
            Console.WriteLine("\nChanges in Solution:");
            for (var i = 1; i < result.Count; i++)
            {

                Console.WriteLine($"\nChanges:\n" +
                                  $"Cost: {result[i].Cost - result[i - 1].Cost : #.##}\n" +
                                  $"SoftObj: {result[i].SoftObjective - result[i - 1].SoftObjective}\n" +
                                  $"Slope: {(result[i].Cost - result[i - 1].Cost)/(result[i].SoftObjective - result[i - 1].SoftObjective) : #.##} ");
                if (result[i - 1].UsedRooms != null && result[i].UsedRooms != null)
                    Console.WriteLine(
                        $"#Rooms: {result[i].UsedRooms.Sum(r => r.Value) - result[i - 1].UsedRooms.Sum(r => r.Value)}");

                if (result[i - 1].Assignments != null && result[i].Assignments != null)
                {
                    int movedTimeslots = 0;
                    var assign1 = result[i - 1].Assignments.GroupBy(a => a.Course).ToDictionary(g => g.Key, g => g.ToList());
                    var assign2 = result[i].Assignments.GroupBy(a => a.Course).ToDictionary(g => g.Key, g => g.ToList());
                    foreach (var course in assign1.Keys)
                    {
                        var t1 = assign1[course].Select(a => a.TimeSlot);
                        var t2 = assign2[course].Select(a => a.TimeSlot);
                        var count = t1.Except(t2).Count();
                        if (count == 0) continue;
                        movedTimeslots += count;
                        // Console.WriteLine(course.ID + ":");
                        // Console.WriteLine(String.Join(",",t1.Except(t2)) + " -> " + string.Join(",",t2.Except(t1)));

                    }
                    int movedRooms = 0;

                    Console.WriteLine(
                        $"Timeslots: {movedTimeslots} ({(double) movedTimeslots/result[i].Assignments.Count: 0.00%} )");
                    if (result[i].Assignments.First().Room != null)
                    {
                        foreach (var course in assign1.Keys)
                        {
                            var r1 = assign1[course].Select(a => a.Room);
                            var r2 = assign2[course].Select(a => a.Room);
                            var count = 0; //r1.Except(r2).Count();

                            foreach (var r in r1.Distinct())
                            {
                               count+= r1.Count(_r => _r == r) - r2.Count(_r => _r == r);
                            }
                            

                            var inter = r1.Intersect(r2);
                            if (count == 0) continue;
                            movedRooms += count;
                            // Console.WriteLine(course.ID + ":");
                            // Console.WriteLine(String.Join(",",t1.Except(t2)) + " -> " + string.Join(",",t2.Except(t1)));

                        }
                        Console.WriteLine(
                            $"Rooms: {movedRooms} ({(double)movedRooms / result[i].Assignments.Count: 0.00%} )");
                    }
                    
                    
                }


            }


        }

    }
}
