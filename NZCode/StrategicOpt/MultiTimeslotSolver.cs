using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversityTimetabling.MIPModels;

namespace UniversityTimetabling.StrategicOpt
{
    public class MultiTimeslotSolver
    {
        private int Timelimit;
        private Data data;
        private ProblemFormulation formulation;
        private CCTModel _model;
        private CCTModel.MIPModelParameters MIPmodelParameters;
        public int formulationtype = -1;

        public MultiTimeslotSolver(int timelimit, Data data, ProblemFormulation formulation,CCTModel.MIPModelParameters mipModelParameters)
        {
            Timelimit = timelimit;
            this.data = data;
            this.formulation = formulation;
            this.MIPmodelParameters = mipModelParameters;
        }

        public List<Tuple<int, int, int, int>> Run()
        {
            if (formulationtype < 0 || formulationtype > 1) throw new ArgumentException("No formulationtype set");
            var stopwatch = Stopwatch.StartNew();
            var solutions = new List<Tuple<int, int, int, int>>();


            //  var newrooms = CreateRoomsFixedSize(data, 25, 1);
            //  data.SetRoomList(newrooms);

            _model = new CCTModel(data, formulation, MIPmodelParameters);

            _model.Optimize(Timelimit);
            var minobj = (int)Math.Round(_model.Objective);

            solutions.Add(Tuple.Create(data.TimeSlots.Count, (int)Math.Round(_model.Objective),
                (int)Math.Ceiling(_model.ObjBound), (int)stopwatch.Elapsed.TotalSeconds));
            //Minimize timeslots
            int maxtimeslots = 0;
            if (formulationtype == 0)
            {
                _model.SetQualConstraint(minobj);
                _model.SetObjective(0, 0, totaltimeslots: 1);
                _model.Optimize(Timelimit);
                _model.SetQualConstraint();
                maxtimeslots = (int) Math.Round(_model.Objective);
                _model.SetObjective(1, 0);
            }
            else if (formulationtype == 1)
            {
                _model.SetBudgetConstraint(minobj);
                _model.SetObjective(0, 0, totaltimeslots: 1);
                _model.Optimize(Timelimit);
                _model.SetBudgetConstraint();
                 maxtimeslots = (int)Math.Round(_model.Objective);
                _model.SetObjective(0, 1);
            }


            for (int nincluded = 0; nincluded < data.TimeSlots.Count; nincluded++)
            {
                if (nincluded > maxtimeslots) break;

                Console.WriteLine($"Nincluded: {nincluded} ({(double)nincluded / data.TimeSlots.Count: 0.00%})");

                _model.SetTimeslotsUsedConstraint(nincluded);
                var sol = _model.Optimize(Timelimit);
                if (sol)
                {
                    solutions.Add(Tuple.Create(nincluded, (int)Math.Round(_model.Objective), (int)Math.Ceiling(_model.ObjBound),
                        (int)stopwatch.Elapsed.TotalSeconds));
                    if ((int)Math.Round(_model.Objective) <= minobj) break;
                }
            }
            //Tighten bounds.
            for (int i = 0; i < solutions.Count; i++)
            {
                var item3 = solutions.Where(s => s.Item1 >= solutions[i].Item1).Max(s => s.Item3);
                solutions[i] = Tuple.Create(solutions[i].Item1, solutions[i].Item2,item3, solutions[i].Item4);
            }


            return solutions;
        }

    }


}
