using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace UniversityTimetabling.MIPModels
{
    public class TimeSlotChoose
    {
        private GRBModel _model;
        private GRBEnv _env;
        public Data Data { get; private set; }
        public ProblemFormulation ProblemFormulation { get; private set; }

        public TimeSlotChoose(Data data, ProblemFormulation formulation)
        {
            Data = data;
            ProblemFormulation = formulation;
            BuildMIPModel();
        }
        private Dictionary<TimeSlot, GRBVar> _varTimeslotUsed;
        private void BuildMIPModel()
        {
            _env = new GRBEnv();
            _model = new GRBModel(_env);
            
          

            _varTimeslotUsed = new Dictionary<TimeSlot, GRBVar>();
            foreach (var timeSlot in Data.TimeSlots)
            {
                _varTimeslotUsed[timeSlot] = _model.AddVar(0, 1, 1, GRB.BINARY, $"t_{timeSlot}");
            }

            _model.Update();
          
           _model.AddConstr(Data.Rooms.Count* _varTimeslotUsed.Values.Sumx(), GRB.GREATER_EQUAL, Data.Courses.Sum(c => c.Lectures),"");
            
            foreach (var roomCapacity in Data.RoomCapacities)
         
            {
                _model.AddConstr(Data.Rooms.Count(r => r.Capacity > roomCapacity.Value)* Data.TimeSlots.Count,GRB.GREATER_EQUAL,
                    Data.Courses.Where(c=> c.NumberOfStudents > roomCapacity.Value).Sum(c => c.Lectures),
                    $"timecapcapcut_{roomCapacity.Value}");
            }

            //Consecutive timeslots
            var timeslotlist = Data.TimeSlots.OrderBy(ts => ts.Period).ThenBy(ts => ts.Day).ToList();
            for (var i = 0; i < timeslotlist.Count - 1; i++)
            {
                _model.AddConstr(_varTimeslotUsed[timeslotlist[i + 1]] - _varTimeslotUsed[timeslotlist[i]], GRB.LESS_EQUAL, 0,
                   $"timeslotconsec_{i}");
            }

            _model.SetCallback(new OutputCallback());

        }

        public void Optimize(double timelimit = GRB.INFINITY, double mipGap = 0.00)
        {
            _model.GetEnv().Set(GRB.DoubleParam.TimeLimit, timelimit);
            _model.GetEnv().Set(GRB.DoubleParam.MIPGap, mipGap);
      

            _model.Optimize();
            if (_model.Get(GRB.IntAttr.Status) == GRB.Status.INFEASIBLE)
            {
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
             
                throw new Exception("Infeasible");
            }
        }
        
    }
}
