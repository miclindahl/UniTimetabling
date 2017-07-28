using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversityTimetabling
{
    /// <summary>
    /// A calculator and validator for solutions to CCT-CB UD2
    /// The majority of the work in this class is made by Niels-Christian Fink Bagger
    /// </summary>
    public class Solution
    {
        public string ResultLine { get; private set; }
        public string TextLine { get; private set; }
        public string ScoreLine { get; private set; }
        public bool IsFeasible { get; private set; }

        public ProblemFormulation Formulation { get; private set; }

        public double UnscheduledLectures { get; private set; }
        public double RoomCapacity { get; private set; }

        public double RoomsUsed { get; private set; }
        public double RoomCost { get; private set; }
        public double MinimumWorkingDays { get; private set; }
        public double CurriculumCompactness { get; private set; }
        public double RoomStability { get; private set; }
        public double StudentMinMaxLoad { get; private set; }
        public double RoomUnsuitability { get; private set; }
        public double BadTimeslots { get; private set; }
        public double Objective { get; private set; }

        public Dictionary<Course,int> PenaltyForCourse { get; private set; } 

        private readonly Data _data;
        public HashSet<Assignment> _assignments;



        public bool RoomAssignmentsExists => _assignments.First().Room != null;

        public bool DoStageIRoomCheck { get; set; } = true;

        public Solution(Data data, ProblemFormulation problemFormulation)
        {
            _data = data;
            Formulation = problemFormulation;
            _assignments = new HashSet<Assignment>();
            IsFeasible = false;
        }

        public void SetAssignments(List<Assignment> assignments)
        {
            _assignments = new HashSet<Assignment>(assignments);
        }
        public void AddAssignment(Assignment assignment)
        {
            _assignments.Add(assignment);
        }

        public void AddAssignment(Course course, TimeSlot timeSlot, Room room)
        {
            AddAssignment(new Assignment(course, timeSlot, room));
        }

        public void Save(string filePath)
        { 
            var dir = new FileInfo(filePath).Directory;
            if (!dir.Exists) dir.Create();

            var sb = new StreamWriter(filePath);
            foreach (var assignment in _assignments)
            {
                sb.WriteLine("{0} {1} {2} {3}", assignment.Course.ID, assignment.Room?.ID ?? "na", assignment.TimeSlot.Day, assignment.TimeSlot.Period);
            }
            sb.Close();
        }

        public void Read(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            // Display the file contents by using a foreach loop.
            foreach (string line in lines)
            {
                var entities = line.Split(' ');
                var course = _data.Courses.Single(c => c.ID == entities[0]);
                var room = _data.Rooms.Single(r => r.ID == entities[1]);
                var timeSlot = _data.TimeSlots.Single(t => t.Day == int.Parse(entities[2]) && t.Period == int.Parse(entities[3]));
                // Use a tab to indent each line of the file.
                AddAssignment(course,timeSlot,room);
            }
        }

        public string AnalyzeSolution()
        {
            var maxCourseSb = new StringBuilder();
            var unavailableSb = new StringBuilder();
            var multipleRoomsSb = new StringBuilder();
            var multipleCoursesSb = new StringBuilder();
            var lecturerSb = new StringBuilder();
            var curriculumSb = new StringBuilder();

            var violations = 0.0;
            Objective = 0.0;

            IsFeasible = false;

            UnscheduledLectures = 0.0;
            RoomCapacity = 0.0;
            MinimumWorkingDays = 0.0;
            CurriculumCompactness = 0.0;
            RoomStability = 0.0;
            StudentMinMaxLoad = 0.0;
            BadTimeslots = 0.0;
            RoomUnsuitability = 0.0;
            RoomCost = 0.0;
            RoomsUsed = 0.0;

            var courseAssignments = _assignments.GroupBy(a => a.Course).ToDictionary(g => g.Key, g => g.ToList());
            var curriculaAssignments = _data.Curricula.ToDictionary(curriculum => curriculum, curriculum => new List<Assignment>());
            var consideredAloneTimeSlot = _data.Courses.ToDictionary(course => course, course => new HashSet<TimeSlot>());

            PenaltyForCourse = new Dictionary<Course, int>();
            foreach (var course in _data.Courses)
            {
                PenaltyForCourse[course] = 0;
            }

            if (!RoomAssignmentsExists && DoStageIRoomCheck) CheckStageIFeasibility();

            foreach (var course in _data.Courses)
            {
                if (!courseAssignments.ContainsKey(course))
                {
                    UnscheduledLectures += course.Lectures;
                    if (course.MinimumWorkingDays > 0) MinimumWorkingDays += course.MinimumWorkingDays;
                    continue;
                }
                if (courseAssignments[course].Count > course.Lectures)
                {
                    maxCourseSb.AppendFormat(
                        "Course {0} has been scheduled for {1} lectures but is only allowed to be scheduled for {2}.\n",
                        course, courseAssignments[course].Count, course.Lectures);

                    violations += courseAssignments[course].Count - course.Lectures;
                }
                else if (courseAssignments[course].Count < course.Lectures)
                {
                    UnscheduledLectures += course.Lectures - courseAssignments[course].Count;
                }

                foreach (var curriculum in course.Curricula)
                {
                    foreach (var assignment in courseAssignments[course])
                        curriculaAssignments[curriculum].Add(assignment);
                }

                var timeSlotAssignments = courseAssignments[course].GroupBy(ca => ca.TimeSlot).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var timeSlotAssign in timeSlotAssignments)
                {
                    BadTimeslots += timeSlotAssign.Key.Cost * timeSlotAssign.Value.Count;
                    PenaltyForCourse[course] += timeSlotAssign.Key.Cost*timeSlotAssign.Value.Count;

                    if (course.UnavailableTimeSlots.Contains(timeSlotAssign.Key))
                    {
                        unavailableSb.AppendFormat(
                            "Course {0} has been scheduled at day {1}, period {2} but this time slot is unavailable for this course.\n",
                            course, timeSlotAssign.Key.Day, timeSlotAssign.Key.Period);

                        violations += 1.0;
                    }

                    if (timeSlotAssign.Value.Count <= 1) continue;

                    multipleRoomsSb.AppendFormat(
                        "Course {0} has been scheduled day {1}, period {2} in rooms {3}\n",
                        course, timeSlotAssign.Key.Day, timeSlotAssign.Key.Period,
                        string.Join(", ", timeSlotAssign.Value.Select(a => a.Room)));

                    violations += timeSlotAssign.Value.Count - 1.0;

                }

                var workDays = timeSlotAssignments.Select(t => t.Key.Day).Distinct().ToList().Count;

                if (workDays < course.MinimumWorkingDays)
                    MinimumWorkingDays += (course.MinimumWorkingDays - workDays);

                if (RoomAssignmentsExists)
                    RoomStability += (courseAssignments[course].Select(a => a.Room).Distinct().Count() - 1);

            }
            foreach (var timeSlotAssigns in _data.Curricula.Select(curriculum => curriculaAssignments[curriculum].Select(a => a.TimeSlot).Distinct().ToList()))
            {
                CurriculumCompactness +=
                    timeSlotAssigns.Count(
                        timeSlotAssign => !timeSlotAssigns.Any(ts => TimeSlot.TimeSlotsAreConsequtive(ts, timeSlotAssign)));
            }
            if (RoomAssignmentsExists)
            { 
            var roomAssignments = _assignments.GroupBy(a => a.Room).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var roomAssign in roomAssignments)
                {
                    var timeSlotAssigns = roomAssign.Value.GroupBy(a => a.TimeSlot)
                        .ToDictionary(g => g.Key, g => g.Distinct().ToList());
                    foreach (var timeSlotAssign in timeSlotAssigns)
                    {
                        var courseAssigns = timeSlotAssign.Value.Select(a => a.Course).Distinct().ToList();
                        if (courseAssigns.Count > 1)
                        {
                            multipleCoursesSb.AppendFormat(
                                "Room {0} has been scheduled at day {1}, period {2} for courses {3}\n",
                                roomAssign.Key, timeSlotAssign.Key.Day, timeSlotAssign.Key.Period,
                                string.Join(", ", courseAssigns));

                            violations += timeSlotAssign.Value.Count - 1.0;
                        }
                        var students = timeSlotAssign.Value.Sum(a => a.Course.NumberOfStudents);
                        if (students > roomAssign.Key.Capacity)
                        {
                            RoomCapacity += students - roomAssign.Key.Capacity;
                        }
                    }
                    if (roomAssign.Value.Count > 0)
                    {
                        RoomsUsed += 1;
                        RoomCost += roomAssign.Key.Cost;
                    }

                    RoomUnsuitability += roomAssign.Value.Count(a => a.Course.UnsuitableRooms.Contains(roomAssign.Key));
                }
            }

            var lecturerAssigns = _assignments.GroupBy(a => a.Course.Lecturer).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var lecturerAssign in lecturerAssigns)
            {
                var timeSlotAssigns = lecturerAssign.Value.GroupBy(l => l.TimeSlot).ToDictionary(g => g.Key, g => g.Distinct().ToList());
                foreach (var timeSlotAssign in timeSlotAssigns)
                {
                    var roomsAssigns = timeSlotAssign.Value.Select(a => a.Course).Distinct().ToList();

                    if (roomsAssigns.Count <= 1) continue;

                    lecturerSb.AppendFormat(
                        "Lecturer {0} has been scheduled at day {1}, period {2} for courses {3}\n",
                        lecturerAssign.Key, timeSlotAssign.Key.Day, timeSlotAssign.Key.Period, string.Join(", ", roomsAssigns));

                    violations += timeSlotAssign.Value.Count - 1.0;
                }
            }


            foreach (var curriculumAssign in curriculaAssignments)
            {
                var timeSlotAssigns = curriculumAssign.Value.GroupBy(cu => cu.TimeSlot).ToDictionary(g => g.Key, g => g.Distinct().ToList());
                foreach (var timeSlotAssign in timeSlotAssigns)
                {
                    var courseAssigns = timeSlotAssign.Value.Select(a => a.Course).Distinct().ToList();

                    if (courseAssigns.Count <= 1) continue;

                    curriculumSb.AppendFormat(
                        "Curriculum {0} has been scheduled at day {1}, period {2} for courses {3}\n",
                        curriculumAssign.Key, timeSlotAssign.Key.Day, timeSlotAssign.Key.Period, string.Join(", ", courseAssigns));

                    violations += timeSlotAssign.Value.Count - 1.0;
                }
                var dayAssign = curriculumAssign.Value.GroupBy(a => a.TimeSlot.Day).ToDictionary(g => g.Key, g => g.Count());
                foreach (var day in dayAssign)
                {
                    var minMaxLoadViol = Math.Max(day.Value - _data.MaximumPeriodsPerDay, 0)
                                            + Math.Max(_data.MinimumPeriodsPerDay - day.Value, 0);
                    StudentMinMaxLoad += minMaxLoadViol;
                    if (minMaxLoadViol > 0.5)
                    {
                     //   Console.WriteLine($"S:StudentloadViol: {curriculumAssign.Key} {day.Key}: {minMaxLoadViol}");
                    }
                }
            }

            var sB = new StringBuilder();

            if (violations > 0.0)
            {
                if (maxCourseSb.Length > 0)
                {
                    sB.AppendLine("Each course is only allowed to be scheduled for a given maximum number of lectures\n");
                    sB.AppendLine(maxCourseSb.ToString());
                }
                if (unavailableSb.Length > 0)
                {
                    if (maxCourseSb.Length > 0) sB.AppendLine();
                    sB.AppendLine("Courses are not allowed to be scheduled in time slots marked as unavailable\n");
                    sB.AppendLine(unavailableSb.ToString());
                }
                if (multipleRoomsSb.Length > 0)
                {
                    if ((maxCourseSb.Length > 0 | unavailableSb.Length > 0)) sB.AppendLine();
                    sB.AppendLine("Each course is only allowed to be scheduled in a single room in each time slot\n");
                    sB.AppendLine(multipleRoomsSb.ToString());
                }
                if (multipleCoursesSb.Length > 0)
                {
                    if ((maxCourseSb.Length | unavailableSb.Length | multipleRoomsSb.Length) > 0) sB.AppendLine();
                    sB.AppendLine("Each room can only accommodate a single course in each time slot\n");
                    sB.AppendLine(multipleCoursesSb.ToString());
                }
                if (lecturerSb.Length > 0)
                {
                    if ((maxCourseSb.Length | unavailableSb.Length | multipleRoomsSb.Length | multipleCoursesSb.Length) > 0) sB.AppendLine();
                    sB.AppendLine("Each lecturer can only teach a single course in each time slot\n");
                    sB.AppendLine(lecturerSb.ToString());
                }
                if (curriculumSb.Length > 0)
                {
                    if ((maxCourseSb.Length | unavailableSb.Length | multipleRoomsSb.Length | multipleCoursesSb.Length | lecturerSb.Length) > 0) sB.AppendLine();
                    sB.AppendLine("Only a single course in a curriculum can be scheduled in each time slot\n");
                    sB.AppendLine(curriculumSb.ToString());
                }

                ResultLine = "RESULT WRONG";
                TextLine = "The solution is infeasible. The number of violations is " + violations;
                ScoreLine = "SCORE " + violations.ToString();
                return sB.ToString();
            }

            IsFeasible = true;

            Objective = 0
                + RoomCapacity * Formulation.RoomCapacityWeight
                + MinimumWorkingDays * Formulation.MinimumWorkingDaysWeight
                + CurriculumCompactness * Formulation.CurriculumCompactnessWeight
                + RoomStability * Formulation.RoomStabilityWeight
                + BadTimeslots * Formulation.BadTimeslotsWeight
                + RoomUnsuitability * Formulation.UnsuitableRoomsWeight
                + StudentMinMaxLoad*Formulation.StudentMinMaxLoadWeight;

            var calculatedWidth = "Violation".Length;

            sB.AppendLine("Table 1: Penalty values");
            sB.AppendLine();
            sB.AppendFormat("Name                  | {0} | {1} | \n", "Violation".PadRight(calculatedWidth),"Obj".PadLeft(calculatedWidth));
            sB.AppendFormat("============================={0}\n", new string('=', calculatedWidth*2),0);
            sB.AppendFormat("UNSCHEDULED           | {0} | {1} |\n", UnscheduledLectures.ToString().PadLeft(calculatedWidth),"".PadRight(calculatedWidth));
            sB.AppendFormat("ROOMCAPACITY          | {0} | {1} |\n", RoomCapacity.ToString().PadLeft(calculatedWidth), (RoomCapacity * Formulation.RoomCapacityWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("MINIMUMWORKINGDAYS    | {0} | {1} |\n", MinimumWorkingDays.ToString().PadLeft(calculatedWidth), (MinimumWorkingDays * Formulation.MinimumWorkingDaysWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("CURRICULUMCOMPACTNESS | {0} | {1} |\n", CurriculumCompactness.ToString().PadLeft(calculatedWidth), (CurriculumCompactness * Formulation.CurriculumCompactnessWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("ROOMSTABILITY         | {0} | {1} |\n", RoomStability.ToString().PadLeft(calculatedWidth), (RoomStability * Formulation.RoomStabilityWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("RoomUnsuitability     | {0} | {1} |\n", RoomUnsuitability.ToString().PadLeft(calculatedWidth), (RoomUnsuitability * Formulation.UnsuitableRoomsWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("StudentMinMaxLoad     | {0} | {1} |\n", StudentMinMaxLoad.ToString().PadLeft(calculatedWidth), (StudentMinMaxLoad * Formulation.StudentMinMaxLoadWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("BadTimeslots          | {0} | {1} |\n", BadTimeslots.ToString().PadLeft(calculatedWidth), (BadTimeslots * Formulation.BadTimeslotsWeight).ToString().PadLeft(calculatedWidth));
            sB.AppendFormat("OBJECTIVE             | {0} | {1} |\n"," ".PadLeft(calculatedWidth),Objective.ToString().PadLeft(calculatedWidth));
            sB.AppendLine();
            sB.AppendLine();
            sB.AppendLine("Table 2: Used Room stat");
            sB.AppendLine();
            sB.AppendLine("Name                  | Value");
            sB.AppendLine("================================================================");
            sB.AppendFormat("Used Rooms           | {0}/{1} ({2:0.00%})\n", RoomsUsed,_data.Rooms.Count, RoomsUsed / _data.Rooms.Count);
            sB.AppendFormat("Room Cost            | {0}/{1} ({2:0.00%})\n", RoomCost, _data.Rooms.Sum(r => r.Cost), RoomCost / _data.Rooms.Sum(r => r.Cost));
           // sB.AppendFormat("Utilization    | Total violation of minimum working days");


            ResultLine = "RESULT CORRECT";
            TextLine = "The solution is feasible. The Objective value is " + Objective;
            ScoreLine = "SCORE " + Objective;
            return sB.ToString();
        }
        private void CheckStageIFeasibility()
        {
            foreach (var timeSlot in _data.TimeSlots)

            {
                var totalint = _assignments.Count(a => Equals(a.TimeSlot, timeSlot));
                if (totalint > _data.Rooms.Count) throw new Exception("too few rooms wtf");
                foreach (var roomCapacity in _data.RoomCapacities)
                {
                    var nrooms = _data.Rooms.Count(r => r.Capacity > roomCapacity.Value);

                    var ncourses =
                        _assignments.Count(
                            a => a.TimeSlot == timeSlot && a.Course.NumberOfStudents > roomCapacity.Value);
                    if (ncourses > nrooms) throw new Exception("too few rooms wtf");
                }
            }
        }

    }

    public class CurriculumAssignment
    {
        public Curriculum Curriculum { get; private set; }
        public TimeSlot TimeSlot { get; private set; }

        public CurriculumAssignment(Curriculum curriculum, TimeSlot timeSlot)
        {
            Curriculum = curriculum;
            TimeSlot = timeSlot;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var assign = obj as CurriculumAssignment;
            if (assign == null) return false;

            return Curriculum.Equals(assign.Curriculum) && TimeSlot.Equals(assign.TimeSlot);
        }

        public override int GetHashCode()
        {
            return Curriculum.GetHashCode() ^ TimeSlot.GetHashCode();
        }
    }

    public class Assignment
    {
        public Course Course { get; private set; }
        public TimeSlot TimeSlot { get; private set; }
        public Room Room { get; private set; }

        public Assignment(Course course, TimeSlot timeSlot, Room room)
        {
            Course = course;
            TimeSlot = timeSlot;
            Room = room;
        }
        public Assignment(Course course, TimeSlot timeSlot)
        {
            Course = course;
            TimeSlot = timeSlot;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var assign = obj as Assignment;
            if (assign == null) return false;

            return Course.Equals(assign.Course) && TimeSlot.Equals(assign.TimeSlot) && Room.Equals(assign.Room);
        }

        public override int GetHashCode()
        {
            return Course.GetHashCode() ^ TimeSlot.GetHashCode() ^ (Room?.GetHashCode() ?? 1);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Course, TimeSlot, Room);
        }
    }


}
