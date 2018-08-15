using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversityTimetabling.MIPModels;

namespace UniversityTimetabling
{
    public class QualityRecoveringOptimizer
    {
        private ProblemFormulation formulation;
        private Data data;
        private CCTModel model;
        private CCTModel.MIPModelParameters MIPmodelParameters;
        private Solution solutionBefore;
        public bool RemoveCurrentSolutionAfteritt = false;
        public int ExtraPerubations = 30;
        public int MaxTotalPertubations = 30;
        public int SolPerPertubation = 1;
        public int Timelimit = 60*60*24;
        public bool PerturbationObjectEqual = false;
        public bool AbortSolverWhenNoImprovement = true;
        public enum DisruptionTypes
        {
            SecondLargestRoomRemovedOneTimeslot,
            SecondLargestRoomDayRemoved,
            RandomRoomRandomdayRemoved,
            OneTimeslotUnavailable,
            OneAssignmentUnavailbility,
            CurriculumFourCoursesInsert,
            CurriculumFourCoursesInsertWithCC,
        }


        public QualityRecoveringOptimizer(Data data, Solution solutionBefore, ProblemFormulation formulation, CCTModel.MIPModelParameters mipModelParameters)
        {
            this.data = data;
            this.formulation = formulation;
            this.MIPmodelParameters = mipModelParameters;
            this.solutionBefore = solutionBefore;

            model = new CCTModel(data, formulation, MIPmodelParameters);
            model.SetProximityOrigin(solutionBefore._assignments.ToList());
            model.SetMipHints(solutionBefore._assignments.ToList());

        }


        public List<Tuple<int, Solution, int, int>> Run()
        {

            var before = (int)solutionBefore.Objective;
           
            model.SetObjective(0, 0, 1);
            //model.Fixsol(false);
            //model.SetProxConstraint(10);
            var feasible = model.Optimize(Timelimit);
            if (!feasible) return new List<Tuple<int, Solution, int, int>>();
            var minperb = (int)model.Objective;

            //Find best objective
            model.SetObjective(1, 0, 0);
            var sol = new List<Tuple<int, Solution, int,int>>();
            var currentObjective = 0;
            var maxPerturbations = Math.Max(minperb + ExtraPerubations, MaxTotalPertubations);
            for (var pertubations = minperb; pertubations <= maxPerturbations; pertubations++)
            {
                Console.WriteLine($"pertubations = {pertubations}");

                model.SetProxConstraint(pertubations);
                if (PerturbationObjectEqual) model.SetProcConstraintSenseEqual(true);
               // if (RemoveCurrentSolutionAfteritt) model.b
                //model.Optimize();
                int sols;
	            for (sols = 0; sols < SolPerPertubation; sols++)
	            {
		            if (!model.Optimize(Timelimit))
		            {
			            if (AbortSolverWhenNoImprovement) break; //infeasibles
			            else continue;
		            }

		            Console.WriteLine("new solution");
		            //constraint on objective function


		            // if (model.ObjSoftCons == prev) break;
		            currentObjective = model.ObjSoftCons;
		            model.SetQualConstraint(currentObjective);
		            var solututionAfter = new Solution(data, formulation);
		            solututionAfter.SetAssignments(model.GetAssignments());
		            Console.WriteLine($"Before: {before}\n" +
		                              $"After: {model.ObjSoftCons}\n" +
		                              $"Perbs: {pertubations}");
		            Console.WriteLine(solutionBefore.AnalyzeSolution());
		            Console.WriteLine(solututionAfter.AnalyzeSolution());
		            var solution = new Solution(data, formulation);
		            solution.SetAssignments(model.GetAssignments());
		            solution.AnalyzeSolution();
		            sol.Add(Tuple.Create(pertubations, solution, model.RunTime, (int) Math.Ceiling(model.ObjBound)));
		            DisplayPertubations(solutionBefore._assignments.ToList(), solututionAfter._assignments.ToList());
		            //to pertubate more.
		            model.RemoveCurrentSolution();
	            }
	            if (sols == 0) break; //previous pertubations didnt find anything
                model.SetQualConstraint(currentObjective - 1);

            }
            return sol;
        }

        public int Disrupt(DisruptionTypes disruption, int seed,out string entitiesdisrupted)
        {
            var random = new Random(seed);
            var disrupted = 0;

            switch (disruption)
            {
                case DisruptionTypes.SecondLargestRoomRemovedOneTimeslot:
                {
                    var tsday2 = new List<TimeSlot>() {data.TimeSlots[random.Next(data.TimeSlots.Count)]};

                    var secondlargestroom = data.Rooms.OrderByDescending(r => r.Capacity).ToList()[1];
                    disrupted =
                        solutionBefore._assignments.Count(
                            a => a.Room.Equals(secondlargestroom) && tsday2.Contains(a.TimeSlot));
                    model.Disrupt(secondlargestroom, tsday2);
	                entitiesdisrupted = $"{secondlargestroom} in {string.Join(",", tsday2)}";
                    Console.WriteLine($"Removed: {secondlargestroom} in {string.Join(",", tsday2)}\n" +
                                      $"Disrupted: {disrupted} ");
                }
                    break;
                case DisruptionTypes.SecondLargestRoomDayRemoved:
                {
                    var rday = random.Next(data.Days);
                    var tsday2 = data.TimeSlots.Where(t => t.Day == rday).ToList();

                    var secondlargestroom = data.Rooms.OrderByDescending(r => r.Capacity).ToList()[1];
                    disrupted =
                        solutionBefore._assignments.Count(
                            a => a.Room.Equals(secondlargestroom) && tsday2.Contains(a.TimeSlot));
                    model.Disrupt(secondlargestroom, tsday2);
	                entitiesdisrupted = $"{secondlargestroom} in {string.Join(",", tsday2)}";

						Console.WriteLine($"Removed: {secondlargestroom} in {string.Join(",", tsday2)}\n" +
                                      $"Disrupted: {disrupted} ");
                }
                    break;
	            case DisruptionTypes.RandomRoomRandomdayRemoved:
	            {
		            Room room = null;
		            List<TimeSlot> tsday2 = null;
		            while (disrupted == 0)
		            {
			            var rday = random.Next(data.Days);
			            tsday2 = data.TimeSlots.Where(t => t.Day == rday).ToList();
			            var rroom = random.Next(data.Rooms.Count);

			            room = data.Rooms[rroom];
			            disrupted =
				            solutionBefore._assignments.Count(
					            a => a.Room.Equals(room) && tsday2.Contains(a.TimeSlot));
		            }
		            model.Disrupt(room, tsday2);
		            entitiesdisrupted = $"{room} in {string.Join(",", tsday2)}";

		            Console.WriteLine($"Removed: {room} in {string.Join(",", tsday2)}\n" +
		                              $"Disrupted: {disrupted} ");

	            }
		            break;
	            case DisruptionTypes.OneTimeslotUnavailable:
	            {
		            TimeSlot timeSlot = null;
		            while (disrupted == 0)
		            {
			            timeSlot = data.TimeSlots[random.Next(data.TimeSlots.Count)];
			            disrupted = solutionBefore._assignments.Count(a => timeSlot.Equals(a.TimeSlot));
		            }

		            model.Disrupt(timeSlot);
	                entitiesdisrupted = $"{string.Join(",", timeSlot)}";

						Console.WriteLine($"Removed: {string.Join(",", timeSlot)}\n" +
                                      $"Disrupted: {disrupted} ");
                }
                    break;
                case DisruptionTypes.OneAssignmentUnavailbility:
                {
                    var assignment =
                        solutionBefore._assignments.ToList()[random.Next(solutionBefore._assignments.Count)];
                    disrupted = 1;
                    model.Disrupt(assignment.Course, assignment.TimeSlot);
	                entitiesdisrupted = $"{string.Join(",", assignment)}";

						Console.WriteLine($"Removed: {string.Join(",", assignment)}\n" +
                                      $"Disrupted: {disrupted} ");
                }
                    break;
	            case DisruptionTypes.CurriculumFourCoursesInsert:
	            {
		            Curriculum newcurriclum = null;

		            while (disrupted == 0)
		            {
			            newcurriclum = new Curriculum("ExtraCur");
			            while (newcurriclum.Courses.Count < 4)
			            {
				            var course = data.Courses[random.Next(data.Courses.Count)];
				            if (newcurriclum.Courses.Contains(course)) continue;
				            newcurriclum.AddCourse(course);
			            }
			            disrupted = solutionBefore._assignments.GroupBy(a => a.TimeSlot)
				            .Sum(ass =>
				            {
					            var count = ass.Select(a => a.Course).ToList().Intersect(newcurriclum.Courses).Count();
					            return count < 2 ? 0 : count;
				            });

		            }


		            model.Disrupt(newcurriclum);
		            entitiesdisrupted = $"{string.Join(",", newcurriclum.Courses)}";

		            Console.WriteLine($"Added curr: {string.Join(",", newcurriclum.Courses)}\n" +
		                              $"Disrupted: {disrupted} ");
	            }
		            break;
	            case DisruptionTypes.CurriculumFourCoursesInsertWithCC:
                {
                    var newcurriclum = new Curriculum("ExtraCur");
                    while (newcurriclum.Courses.Count < 4)
                    {
                        var course = data.Courses[random.Next(data.Courses.Count)];
                        if (newcurriclum.Courses.Contains(course)) continue;
                        newcurriclum.AddCourse(course);
                    }
                    disrupted = solutionBefore._assignments.GroupBy(a => a.TimeSlot).Sum(ass =>
                    {
                        var count = ass.Select(a => a.Course).ToList().Intersect(newcurriclum.Courses).Count();
                        return count < 2 ? 0 : count;
                    });
                    model.Disrupt(newcurriclum,true);
	                entitiesdisrupted = $"{string.Join(",", newcurriclum.Courses)}";
						Console.WriteLine($"Added curr: {string.Join(",", newcurriclum.Courses)}\n" +
                                      $"Disrupted: {disrupted} ");
                }
                    break;
                default:
                    throw new ArgumentException("unknown disruption " + disruption);
            }
            return disrupted;
        }

        public int DisruptAssignmentUnavailable(int seed, int n)
        {
            if (n > solutionBefore._assignments.Count) throw new ArgumentException("cannot unavailble more than there exists");
            var random = new Random(seed);
            var disrupted = 0;

            var disruptedAssignments = new HashSet<Assignment>();
            while (disrupted < n)
            {
                var assignment = solutionBefore._assignments.ToList()[random.Next(solutionBefore._assignments.Count)];
                if (!disruptedAssignments.Contains(assignment))
                {
                    model.Disrupt(assignment.Course, assignment.TimeSlot);
                    disrupted++;
             
                }
            }

            Console.WriteLine($"Removed: {string.Join(",", disruptedAssignments)}\n" +
                 $"Disrupted: {disrupted} ");
            return disrupted;
        }


        public void DisplayPertubations(List<Assignment> before, List<Assignment> after)
        {
            var unassigned = before.Except(after).ToList();
            var assigned = after.Except(before).ToList();

            var assignmentsBefore = new Dictionary<Course, List<Assignment>>();
            var assignmentsAfter = new Dictionary<Course, List<Assignment>>();

            var coursesAffected = unassigned.Select(a => a.Course).Concat(assigned.Select(a => a.Course)).Distinct().ToList();

            foreach (var course in coursesAffected)
            {
                assignmentsBefore.Add(course, new List<Assignment>());
                assignmentsBefore[course].AddRange(unassigned.Where(a => Equals(a.Course, course)));

                assignmentsAfter.Add(course, new List<Assignment>());
                assignmentsAfter[course].AddRange(assigned.Where(a => Equals(a.Course, course)));
            }

            Console.WriteLine("Pertubations:");
            foreach (var course in coursesAffected)
            {
                Console.WriteLine(course.ID);
                Console.WriteLine("\t" + string.Join(" ; ", assignmentsBefore[course].Select(a => a.TimeSlot + "-" + a.Room).OrderBy(a => a)));
                Console.WriteLine("\t -->");
                Console.WriteLine("\t" + string.Join(" ; ", assignmentsAfter[course].Select(a => a.TimeSlot + "-" + a.Room).OrderBy(a => a)));
            }

            Console.WriteLine("Json");
            Console.WriteLine("pertubations = [");

            foreach (var course in coursesAffected)
            {
                //course room day time isNew
                foreach (var ass in assignmentsBefore[course])
                {
                   Console.WriteLine($"[\"{course}\",\"{ass.Room.ID}\",{ass.TimeSlot.Day},{ass.TimeSlot.Period},0],");
                }
                foreach (var ass in assignmentsAfter[course])
                {
                    Console.WriteLine($"[\"{course}\",\"{ass.Room.ID}\",{ass.TimeSlot.Day},{ass.TimeSlot.Period},1],");

                }
            }
            Console.WriteLine("];");

        }
    }
}
