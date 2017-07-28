using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversityTimetabling.StrategicOpt
{
    public class RoomCombinations
    {
        private List<Tuple<int, int>> _lecturesizes;
        private Dictionary<int, int> _roomsizesMin;
        public int _minCost;
        private List<KeyValuePair<int, int>> _roombase;
        public Data Data { get; private set; }

        public RoomCombinations(Data data, int intervalsize = 1)
        {
            Data = data;
            _lecturesizes =
                data.Courses.GroupBy(c => c.NumberOfStudents)
                    .Select(g => Tuple.Create(g.Key, g.Sum(c => c.Lectures)))
                    .OrderByDescending(t => t.Item1)
                    .ToList();
            var weeklyhours = Data.TimeSlots.Count;
            //calculate minimum
            _roomsizesMin = new Dictionary<int, int>();
            var hoursleft = 0;
            foreach (var size in _lecturesizes)
            {
              //  Console.WriteLine(size.Item1 + " " + size.Item2);
                if (hoursleft < size.Item2)
                {
                    var roomtoadd = (int) Math.Ceiling((double) (size.Item2 - hoursleft)/weeklyhours);
                    _roomsizesMin.Add(size.Item1, roomtoadd);
                    hoursleft += weeklyhours*roomtoadd;
                }
                else _roomsizesMin.Add(size.Item1, 0);

                hoursleft -= size.Item2;
            }
            _minCost = _roomsizesMin.Sum(r => r.Key * r.Value);

            _roombase = _roomsizesMin.ToList();
            _roombase.Add(new KeyValuePair<int, int>(0, 999));
            _roombase = _roombase.OrderBy(kv => kv.Key).ToList();

            /*
            Console.WriteLine("\nRoomSizes: ");
            foreach (var size in _roomsizesMin)
            {
                Console.WriteLine(size.Key + " " + size.Value);
            }
            Console.WriteLine("Total seats: " + _minCost);
            */


            if (intervalsize > 1)
            {
                var _roombaseInt = new List<KeyValuePair<int, int>>();
                foreach (var g in _roombase.GroupBy(r => (int) Math.Ceiling((double) r.Key/intervalsize)*intervalsize))
                {
                    _roombaseInt.Add(new KeyValuePair<int, int>(g.Key, g.Sum(gx => gx.Value)));
                }
                // Console.WriteLine(_roombaseInt);
                _roombase = _roombaseInt;
                _minCost = _roombase.Sum(kv => kv.Key*kv.Value);

                /*
                Console.WriteLine("Intervalazing:");

                Console.WriteLine("\nRoomSizes: ");
                foreach (var size in _roombase)
                {
                    Console.WriteLine(size.Key + " " + size.Value);
                }
                Console.WriteLine("Total seats: " + _minCost);
                */
            }

        }

        public List<Dictionary<int, int>> GetCombinations(int budget)
        {
            if (budget < _minCost) throw new ArgumentException("budget cant be smaller than minimum cost");
            var excess = budget - _minCost;


            //Calculate possible "upgrades"        
            var combinations = roombcombs_up(excess);
            var roomcombs = new List<Dictionary<int, int>>();
            foreach (var combination in combinations)
            {
                var newcomb = _roombase.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);
                foreach (var change in combination)
                {
                    var i = (change.Item3)/(change.Item2-change.Item1);
                    newcomb[change.Item1] -= i;
                    newcomb[change.Item2] += 1;
                }
                newcomb.Remove(0);
                roomcombs.Add(newcomb);

            }

            return roomcombs;
            // return sum_up(potentials, excess);
        }

        private List<List<Tuple<int, int, int>>> roombcombs_up(int target)
        {
            return roombcombs_up(0,1, target, new List<Tuple<int, int,int>>(),0);
        }

        private List<List<Tuple<int, int, int>>> roombcombs_up(int _i, int _j, int target, List<Tuple<int, int,int>> partial, int partialSum)
        {
            Debug.Assert(partial.Sum(p => p.Item3) == partialSum,"tracking variable to avoid recalculing the entire array all the time");

            if (partialSum == target)
            {
                //Console.WriteLine("sum(" + string.Join(",", partial.Select(t => $"{t.Item1} => {t.Item2}")) + ")=" + target);
                return new List<List<Tuple<int, int, int>>>() { partial };
            }

            Debug.Assert(partialSum < target,"this should be the case here");

            var allcombs = new List<List<Tuple<int, int, int>>>();
            for (var i = _i; i < _roombase.Count; i++)
            {
                if (_roombase[i].Value == 0) continue; //no room to upgrade

                //Console.WriteLine(_roombase[i].Value);
                //todo need somehow to also take acount that there can be two rooms of same size etc.

                for (int j = (i == _i ? _j : i+1); j < _roombase.Count; j++)
                {
                    var stepsize = _roombase[j].Key - _roombase[i].Key;
                    if (stepsize > target - partialSum) break;

                    //  Console.WriteLine($"{roombase[i].Key} => {roombase[j].Key}");
                    var partial_rec = new List<Tuple<int, int, int>>(partial);
                    for (int k = 1; k <= _roombase[i].Value; k++)
                    {
                        if (k*stepsize > target - partialSum) break;
                        //Console.WriteLine("using room: " + k);
                        partial_rec.Add(Tuple.Create(_roombase[i].Key, _roombase[j].Key, stepsize));
                        allcombs.AddRange(roombcombs_up(i, j + 1, target, partial_rec, partialSum + k*stepsize));
                    }
                }
            }
            return allcombs;
        }
 
    }
}