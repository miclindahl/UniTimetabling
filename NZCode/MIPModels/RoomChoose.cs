using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace UniversityTimetabling.MIPModels
{
    public class RoomChoose
    {
        private GRBModel _model;
        private GRBEnv _env;
        public Data Data { get; private set; }
        public ProblemFormulation ProblemFormulation { get; private set; }

        public double OverCapacity;

        public RoomChoose(Data data, ProblemFormulation formulation, double overcapacity = 0)
        {
            Data = data;
            ProblemFormulation = formulation;
            OverCapacity = overcapacity;
            BuildMIPModel();
        }
        private Dictionary<Room, GRBVar> varR;
        private void BuildMIPModel()
        {
            _env = new GRBEnv();
            _model = new GRBModel(_env);
            

            //room used.
            varR = new Dictionary<Room, GRBVar>();
            foreach (var room in Data.Rooms)
            {
                varR[room] = _model.AddVar(0, GRB.INFINITY, room.Cost, GRB.INTEGER,
                    $"r_{room}");
            }

            _model.Update();
           
           

           _model.AddConstr(Data.TimeSlots.Count*varR.Values.Sumx(), GRB.GREATER_EQUAL, Data.Courses.Sum(c => c.Lectures),"");
            
            foreach (var roomCapacity in Data.RoomCapacities)
            {
                var overCap = OverCapacity*((double) Data.Courses.Where(c => c.NumberOfStudents > roomCapacity.Value).Sum(c => c.Lectures)/Data.TimeSlots.Count);
                _model.AddConstr(varR.Where(r => r.Key.Capacity > roomCapacity.Value).Sumx(r => r.Value)* Data.TimeSlots.Count, GRB.GREATER_EQUAL,
                    Data.Courses.Where(c=> c.NumberOfStudents > roomCapacity.Value).Sum(c => c.Lectures)+ overCap,
                    $"roomcapcut_{roomCapacity.Value}");
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

        public Dictionary<Room,int> GetUsedRooms()
        {
            var usedRooms = new Dictionary<Room,int>();
            foreach (var room in Data.Rooms)
            {
                if (varR[room].Get(GRB.DoubleAttr.X) < 0.5) continue;
                usedRooms[room] = (int) Math.Round(varR[room].Get(GRB.DoubleAttr.X));
            }
            return usedRooms;
        }
    }
}
