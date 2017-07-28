using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniversityTimetabling
{
    public delegate void OnHashSetAddOrRemove<T>(T element);
    public delegate void OnHashSetClear();
    public delegate void OnHashSetChange();
    public delegate void OnHashSetRemoveWhere<T>(Predicate<T> match);

    public class ObservableHashSet<T> : HashSet<T>
    {
        public event OnHashSetAddOrRemove<T> OnAdd;
        public event OnHashSetAddOrRemove<T> OnRemove;
        public event OnHashSetClear OnClear;
        public event OnHashSetChange OnChange;
        public event OnHashSetRemoveWhere<T> OnRemoveWhere;

        public new bool Add(T element)
        {
            var result = base.Add(element);
            if (result && OnAdd != null) OnAdd(element);
            return result;
        }

        public new bool Remove(T element)
        {
            var result = base.Remove(element);
            if (result && OnRemove != null) OnRemove(element);
            return result;
        }

        public new void Clear()
        {
            base.Clear();
            if (OnClear != null) OnClear();
        }

        public new void ExceptWith(IEnumerable<T> other)
        {
            base.ExceptWith(other);
            if (OnChange != null) OnChange();
        }

        public new void IntersectWith(IEnumerable<T> other)
        {
            base.IntersectWith(other);
            if (OnChange != null) OnChange();
        }

        public new int RemoveWhere(Predicate<T> match)
        {
            var result = base.RemoveWhere(match);
            if (OnRemoveWhere != null) OnRemoveWhere(match);
            return result;
        }

        public new void SymmetricExceptWith(IEnumerable<T> other)
        {
            base.SymmetricExceptWith(other);
            if (OnChange != null) OnChange();
        }

        public new void UnionWith(IEnumerable<T> other)
        {
            base.UnionWith(other);
            if (OnChange != null) OnChange();
        }

        public ObservableHashSet() : base() { }

        public ObservableHashSet(IEnumerable<T> collection) : base(collection) { }

        public ObservableHashSet(IEqualityComparer<T> comparer) : base(comparer) { }
        public ObservableHashSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : base(collection, comparer) { }
    }



    public partial class Data
    {
        public const double UnscheduledLecturesWeight = 10.0;
        public const double RoomCapacityWeight = 1.0;
        public const double MinimumWorkingDaysWeight = 5.0;
        public const double CurriculumCompactnessWeight = 2.0;
        public const double RoomStabilityWeight = 1.0;

        public string Name { get; set; }

        public string Filename { get; set; }

        public List<Lecturer> Lecturers { get; private set; }
        public List<Course> Courses { get; private set; }
        public List<Room> Rooms { get; private set; }
        public List<RoomCapacity> RoomCapacities { get; private set; }

        public List<Building> Buildings { get; private set; }
        public List<Curriculum> Curricula { get; private set; }
        public List<TimeSlot> TimeSlots { get; private set; }
        public int Days { get; private set; }
        public int Periods { get; set; }
		public int TimeLimit { get; private set; }

        public int Threads { get; set; }

        public bool RecalculateTimeSlotsByDay { get; set; }
        private Dictionary<int, HashSet<TimeSlot>> timeSlotsByDay;
        public Dictionary<int, HashSet<TimeSlot>> TimeSlotsByDay
        {
            get
            {
                if (timeSlotsByDay == null || RecalculateTimeSlotsByDay)
                {
                    timeSlotsByDay = new Dictionary<int, HashSet<TimeSlot>>();
                    for (int d = 0; d < Days; d++) timeSlotsByDay.Add(d, new HashSet<TimeSlot>());
                    foreach (var timeSlot in TimeSlots) timeSlotsByDay[timeSlot.Day].Add(timeSlot);
                    RecalculateTimeSlotsByDay = false;
                }
                return timeSlotsByDay;
            }
        }

        public int MinimumPeriodsPerDay { get; private set; }
        public int MaximumPeriodsPerDay { get; private set; }

        public Data()
        {
            Lecturers = new List<Lecturer>();
            Courses = new List<Course>();
            Rooms = new List<Room>();
            RoomCapacities = new List<RoomCapacity>();
            Curricula = new List<Curriculum>();
            TimeSlots = new List<TimeSlot>();
            Days = 0;
            Periods = 0;
	        TimeLimit = 60;
        }

        public Data(IEnumerable<Course> courses, IEnumerable<Lecturer> lecturers, IEnumerable<Room> rooms,
            IEnumerable<Curriculum> curricula, IEnumerable<TimeSlot> timeSlots, int days, int periods,
            int timelimit = 60) : this(courses, lecturers, rooms, curricula, timeSlots, days, periods, 0, 0, timelimit)
        {
            
        }

        public Data(IEnumerable<Course> courses, IEnumerable<Lecturer> lecturers, IEnumerable<Room> rooms, IEnumerable<Curriculum> curricula, IEnumerable<TimeSlot> timeSlots, int days, int periods, int minPeriodsPerDay, int maxPeriodsPerDay, int timelimit = 60)
        {
            Courses = new List<Course>(courses);
            Lecturers = new List<Lecturer>(lecturers);
            Rooms = new List<Room>(rooms);
         
            UpdateRoomData();

            Curricula = new List<Curriculum>(curricula);
            TimeSlots = new List<TimeSlot>(timeSlots);
            Days = days;
            MinimumPeriodsPerDay = minPeriodsPerDay;
            MaximumPeriodsPerDay = maxPeriodsPerDay;
            Periods = periods;
	        TimeLimit = timelimit;
        }

        public void CleanRemovedCourses(List<Course> courses)
        {
            foreach (var course in courses)
            {
                Courses.Remove(course);
                foreach (var curriculum in Curricula)
                {
                    curriculum.Courses.Remove(course);
                }
                foreach (var lecturer in Lecturers)
                {
                    lecturer.Courses.Remove(course);
                }
            }

        }

        public void SetRoomList(List<Room> rooms)
        {
            Rooms = rooms;
            UpdateRoomData();
        }

        private void UpdateRoomData()
        {

            if (Rooms.All(r => r.Building != null))
            {
                Buildings = Rooms.Select(r => r.Building).Distinct().ToList();
            }
            RoomCapacities = new List<RoomCapacity>();

            var caps = Rooms.Select(r => r.Capacity).Distinct();
            foreach (var capacity in caps)
            {
                RoomCapacities.Add(new RoomCapacity(Rooms, capacity));
            }
        }

        public static void PrintAssignment(Course c, TimeSlot ts, Room r)
        {
            Console.WriteLine("{0} {1} {2}", c, ts, r);
        }

        public static Data Read(string[] args)
        {
            if (args.Length != 8) throw new ArgumentException("The input must be an array of strings of size 8");
            return Read(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
        }

        /// <summary>
        /// This method reads text files from the given input and constructs the data objects
        /// </summary>
        /// <param name="directory">The directory containing the files</param>
        /// <param name="basicFile">The file with basic information usually named basic.utt</param>
        /// <param name="coursesFile">The file containing all the courses usually named courses.utt</param>
        /// <param name="lecturersFile">The file containing a list of all the lecturers usually named lecturers.utt</param>
        /// <param name="roomsFile">The file containing the information about the rooms usually named rooms.utt</param>
        /// <param name="curriculaFile">The file containing all curricula usually named curricula.utt</param>
        /// <param name="relationFile">The file containing the relations between the curricula and the courses usually named relation.utt</param>
        /// <param name="unavailabilityFile">The file containing information on the time slots that the courses are unavailable usually named unavailability.utt</param>
        /// <param name="timelimit">The limit of time in seconds that the solver is allowed to run</param>
        /// <returns></returns>
        public static Data ReadDir(string directory, string basicFile, string coursesFile, string lecturersFile, string roomsFile, string curriculaFile, string relationFile, string unavailabilityFile, string timelimit = "60")
        {
            return Read(Path.Combine(directory, basicFile), Path.Combine(directory, coursesFile),
                Path.Combine(directory, lecturersFile), Path.Combine(directory, roomsFile),
                Path.Combine(directory, curriculaFile), Path.Combine(directory, relationFile),
                Path.Combine(directory, unavailabilityFile), timelimit);
        }

        /// <summary>
        /// This method reads text files from the given input and constructs the data objects
        /// </summary>
        /// <param name="basicFile">The file with basic information usually named basic.utt</param>
        /// <param name="coursesFile">The file containing all the courses usually named courses.utt</param>
        /// <param name="lecturersFile">The file containing a list of all the lecturers usually named lecturers.utt</param>
        /// <param name="roomsFile">The file containing the information about the rooms usually named rooms.utt</param>
        /// <param name="curriculaFile">The file containing all curricula usually named curricula.utt</param>
        /// <param name="relationFile">The file containing the relations between the curricula and the courses usually named relation.utt</param>
        /// <param name="unavailabilityFile">The file containing information on the time slots that the courses are unavailable usually named unavailability.utt</param>
        /// <param name="timelimit">The limit of time in seconds that the solver is allowed to run</param>
        /// <returns></returns>
        public static Data Read(string basicFile, string coursesFile, string lecturersFile, string roomsFile, string curriculaFile, string relationFile, string unavailabilityFile, string timelimit = "60")
        {
            var basicElements = File.ReadAllLines(basicFile)[1].Split(' ');
            if (basicElements.Length != 7) throw new ArgumentException("The basic information must consist of 7 integers");

            var nCourses = int.Parse(basicElements[0]);
            var nRooms = int.Parse(basicElements[1]);
            var nDays = int.Parse(basicElements[2]);
            var nPeriods = int.Parse(basicElements[3]);
            var nCurricula = int.Parse(basicElements[4]);
            var nConstraints = int.Parse(basicElements[5]);
            var nLecturers = int.Parse(basicElements[6]);

            var lecturers = new HashSet<Lecturer>(from line in File.ReadAllLines(lecturersFile).Skip(1)
                                                  where !string.IsNullOrEmpty(line)
                                                  select new Lecturer(line));

            var timeSlots = new HashSet<TimeSlot>(from day in Enumerable.Range(0, nDays)
                                                  from period in Enumerable.Range(0, nPeriods)
                                                  select new TimeSlot(day, period));

            var rooms = new HashSet<Room>(from line in File.ReadAllLines(roomsFile).Skip(1)
                                          where !string.IsNullOrEmpty(line)
                                          let elements = line.Split(' ')
                                          select new Room(elements[0], int.Parse(elements[1])));

            var curricula = (from line in File.ReadAllLines(curriculaFile).Skip(1)
                             where !string.IsNullOrEmpty(line)
                             let elements = line.Split(' ')
                             select new Curriculum(elements[0])).ToDictionary(q => q.ID);

            var courses = (from line in File.ReadAllLines(coursesFile).Skip(1)
                           where !string.IsNullOrEmpty(line)
                           let elements = line.Split(' ')
                           select new Course(
                               elements[0],
                               lecturers.First(l => l.ID.CompareTo(elements[1]) == 0),
                               int.Parse(elements[2]),
                               int.Parse(elements[3]),
                               int.Parse(elements[4]))).ToDictionary(c => c.ID);

            foreach (var line in File.ReadAllLines(unavailabilityFile).Skip(1).Where(l => !string.IsNullOrEmpty(l)))
            {
                var elems = line.Split(' ');
                var day = int.Parse(elems[1]);
                var period = int.Parse(elems[2]);
                courses[elems[0]].Add(timeSlots.First(ts => ts.Day == day && ts.Period == period));
            }

            var relations = from line in File.ReadAllLines(relationFile).Skip(1)
                            where !string.IsNullOrEmpty(line)
                            let elements = line.Split(' ')
                            select new
                            {
                                Curriculum = curricula[elements[0]],
                                Course = courses[elements[1]]
                            };

            foreach (var relation in relations)
            {
                relation.Curriculum.AddCourse(relation.Course);
                relation.Course.Add(relation.Curriculum);
            }

            foreach (var course in courses.Values)
            {
                course.Lecturer.Add(course);
            }

	        int timeLimit;
			if (!int.TryParse(timelimit, out timeLimit))
			{
				timeLimit = 60;
			}
			
            var data = new Data(courses.Values, lecturers, rooms, curricula.Values, timeSlots, nDays, nPeriods, timeLimit);

            //Check the counts
            if (nCourses != data.Courses.Count)
                throw new ArgumentException(string.Format("The basic information states there are {0} courses but {1} was read", nCourses, data.Courses.Count));
            if (nRooms != data.Rooms.Count)
                throw new ArgumentException(string.Format("The basic information states there are {0} rooms but {1} was read", nRooms, data.Rooms.Count));
            if (nDays * nPeriods != data.TimeSlots.Count)
                throw new ArgumentException(string.Format("The basic information states there are {0} days and {1} periods giving {2} time slots but {3} was created", nDays, nPeriods, nDays * nPeriods, data.TimeSlots.Count));
            if (nCurricula != data.Curricula.Count)
                throw new ArgumentException(string.Format("The basic information states there are {0} curricula but {1} was read", nCurricula, data.Curricula.Count));
            var constraints = data.Courses.Sum(c => c.UnavailableTimeSlots.Count);
            if (nConstraints != constraints)
                throw new ArgumentException(string.Format("The basic information states there are {0} unavailability constraints but {1} was read", nConstraints, constraints));
            if (nLecturers != data.Lecturers.Count)
                throw new ArgumentException(string.Format("The basic information states there are {0} lecturers but {1} was read", nLecturers, data.Lecturers.Count));

            return data;
        }

        public Dictionary<Course, Dictionary<TimeSlot, Dictionary<Room, double>>> ReadSolution(string path)
        {
            var result = new Dictionary<Course, Dictionary<TimeSlot, Dictionary<Room, double>>>();

            foreach (var course in Courses)
            {
                result.Add(course, new Dictionary<TimeSlot, Dictionary<Room, double>>());
                foreach (var timeslot in TimeSlots)
                {
                    result[course].Add(timeslot, new Dictionary<Room, double>());
                    foreach (var room in Rooms)
                    {
                        result[course][timeslot].Add(room, 0.0);
                    }
                }
            }

            foreach(var line in File.ReadLines(path))
            {
                var elements = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (elements.Length != 4) continue;

                var course = Courses.First(c => c.ID.CompareTo(elements[0]) == 0);
                var room = Rooms.First(r => r.ID.CompareTo(elements[1]) == 0);
                var timeslot = TimeSlots.First(t => t.Day == int.Parse(elements[2]) && t.Period == int.Parse(elements[3]));

                result[course][timeslot][room] += 1;
            }
            return result;
        } 
    }

    public class ProblemFormulation
    {
        public double RoomCapacityWeight { get; set; } = 1.0;
        public double MinimumWorkingDaysWeight { get; set; } = 5.0;
        public double CurriculumCompactnessWeight { get; set; } = 2.0;
        public double RoomStabilityWeight { get; set; } = 1.0;
        public double OverbookingAllowed { get; set; } = 0.00;
        public double RoomBudget { get; set; } = 1e100;
        public double RoomCostWeight { get; set; } = 1;
        public double StudentMinMaxLoadWeight { get; set; } = 0.00;
        public bool AvailabilityHardConstraint { get; set; }
        public double BadTimeslotsWeight { get; set; } = 0.00;
        public double UnsuitableRoomsWeight { get; set; } = 0.00;

        public ProblemFormulation(double roomCapacityWeight, double minimumWorkingDaysWeight, double curriculumCompactnessWeight, double roomStabilityWeight, double overbookingAllowed, double roomBudget, double roomCostWeight, double studentMinMaxLoadWeight, bool unavailabilityHardConstraint = true)
        {
            RoomCapacityWeight = roomCapacityWeight;
            MinimumWorkingDaysWeight = minimumWorkingDaysWeight;
            CurriculumCompactnessWeight = curriculumCompactnessWeight;
            RoomStabilityWeight = roomStabilityWeight;
            OverbookingAllowed = overbookingAllowed;
            RoomBudget = roomBudget;
            RoomCostWeight = roomCostWeight;
            StudentMinMaxLoadWeight = studentMinMaxLoadWeight;
            AvailabilityHardConstraint = unavailabilityHardConstraint;
        }

        public static ProblemFormulation UD2 => new ProblemFormulation(1,5,2,1,double.PositiveInfinity,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoCurrCompact => new ProblemFormulation(1,5,0,1,double.PositiveInfinity,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoMWD => new ProblemFormulation(1,0,2,1,double.PositiveInfinity,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoOverbook =>  new ProblemFormulation(1,5,2,1,0,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoCurrCompactNoOverbook => new ProblemFormulation(1,5,0,1,0,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoMWDNoOverbook => new ProblemFormulation(1,0,2,1,0,double.PositiveInfinity,0,0);
        public static ProblemFormulation UD2NoNoOverbookNoUnAvilability => new ProblemFormulation(1,5,2,1,0,double.PositiveInfinity,0,0,false);
        public static ProblemFormulation UD2NoCurrCompactNoOverbookWithMinMaxLoad => new ProblemFormulation(1,5,0,1,0,double.PositiveInfinity,0,1);
        public static ProblemFormulation MinimizeRoomCost => new ProblemFormulation(0,0,0,0,0,double.PositiveInfinity,1,0);
        public static ProblemFormulation EverythingZero => new ProblemFormulation(0,0,0,0,0,double.PositiveInfinity,0,0);


        public enum Objective
        {
            RoomCapacity = 1,
            MinimumWorkingDaysWeight,
            CurriculumCompactnessWeight,
            RoomStabilityWeight,
            StudentMinMaxLoadWeight,
            BadTimeslots,
        }

        public void SetWeight(Objective obj, double value)
        {
            switch (obj)
            {
                case Objective.RoomCapacity:
                    RoomCapacityWeight = value;
                    break;
                case Objective.MinimumWorkingDaysWeight:
                    MinimumWorkingDaysWeight = value;
                    break;
                case Objective.CurriculumCompactnessWeight:
                    CurriculumCompactnessWeight = value;
                    break;
                case Objective.StudentMinMaxLoadWeight:
                    StudentMinMaxLoadWeight = value;
                    break;
                case Objective.BadTimeslots:
                    BadTimeslotsWeight = value;
                    break;
                case Objective.RoomStabilityWeight:
                    RoomStabilityWeight = value;
                    break;
                default:
                    throw new ArgumentException("Not implemented / unknown obj: " + obj);
            }
        }


    }

    public class Lecturer
    {
        public string ID { get; private set; }
        public HashSet<Course> Courses { get; private set; }

        public Lecturer(string id)
        {
            ID = id;
            Courses = new HashSet<Course>();
        }

        public void Add(Course c)
        {
            Courses.Add(c);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var l = obj as Lecturer;
            if (l == null) return false;
            return ID.CompareTo(l.ID) == 0;
        }

        public override string ToString()
        {
            return ID;
        }

        public override int GetHashCode()
        {
            return ID == null ? 0 : ID.GetHashCode();
        }
    }
    public class Course
    {
        public string ID { get; private set; }
        public Lecturer Lecturer { get; private set; }
        public int Lectures { get; private set; }
        public int MinimumWorkingDays { get; private set; }
        public int NumberOfStudents { get; private set; }
        public bool DoubleLectures { get; private set; }
        public ObservableHashSet<TimeSlot> UnavailableTimeSlots { get; private set; }
        public Dictionary<int, HashSet<int>> UnavailableDayPeriods { get; private set; }

        public HashSet<Room> UnsuitableRooms { get; private set; } 
        public HashSet<Curriculum> Curricula { get; private set; }

        private Curriculum _maxcurriculum;
        public Curriculum MaxCurriculum
        {
            get
            {
                if (_maxcurriculum == null)
                {
                    _maxcurriculum = Curricula.First();
                    foreach (var curriculum in Curricula)
                    {
                        if (curriculum.Courses.Count > _maxcurriculum.Courses.Count)
                        {
                            _maxcurriculum = curriculum;
                        }
                    }
                }
                return _maxcurriculum;
            }
        }

        public Course(string id, Lecturer lecturer, int lectures, int minimumWorkingDays, int numberOfStudents,
            IEnumerable<TimeSlot> unavailableTimeSlots)
            : this(id, lecturer, lectures, minimumWorkingDays, numberOfStudents, false, unavailableTimeSlots)
        {
        }

        public Course(string id, Lecturer lecturer, int lectures, int minimumWorkingDays, int numberOfStudents, bool doubleLectureses, IEnumerable<TimeSlot> unavailableTimeSlots)
        {
            ID = id;
            Lecturer = lecturer;
            Lectures = lectures;
            MinimumWorkingDays = minimumWorkingDays;
            NumberOfStudents = numberOfStudents;
            DoubleLectures = doubleLectureses;
            UnavailableTimeSlots = new ObservableHashSet<TimeSlot>();
            UnavailableDayPeriods = new Dictionary<int, HashSet<int>>();

            UnavailableTimeSlots.OnAdd += OnAddTimeSlot;
            UnavailableTimeSlots.OnRemove += OnRemoveTimeSlot;

            foreach (var timeSlot in unavailableTimeSlots)
            {
                UnavailableTimeSlots.Add(timeSlot);
            }

            UnsuitableRooms = new HashSet<Room>();
            Curricula = new HashSet<Curriculum>();
        }

        public Course(string id, Lecturer lecturer, int lectures, int minimumWorkingDays, int numberOfStudents, bool doubleLectures = false)
        {
            ID = id;
            Lecturer = lecturer;
            Lectures = lectures;
            MinimumWorkingDays = minimumWorkingDays;
            NumberOfStudents = numberOfStudents;
            DoubleLectures = doubleLectures;
            UnavailableTimeSlots = new ObservableHashSet<TimeSlot>();
            UnavailableDayPeriods = new Dictionary<int, HashSet<int>>();

            UnavailableTimeSlots.OnAdd += OnAddTimeSlot;
            UnavailableTimeSlots.OnRemove += OnRemoveTimeSlot;

            UnsuitableRooms = new HashSet<Room>();
            Curricula = new HashSet<Curriculum>();
        }

        private void OnRemoveTimeSlot(TimeSlot timeSlot)
        {
            if (UnavailableDayPeriods.ContainsKey(timeSlot.Day))
            {
                UnavailableDayPeriods[timeSlot.Day].Remove(timeSlot.Period);
                if (UnavailableDayPeriods[timeSlot.Day].Count == 0)
                {
                    UnavailableDayPeriods.Remove(timeSlot.Day);
                }
            }
        }

        private void OnAddTimeSlot(TimeSlot timeSlot)
        {
            if (!UnavailableDayPeriods.ContainsKey(timeSlot.Day))
            {
                UnavailableDayPeriods.Add(timeSlot.Day, new HashSet<int> { timeSlot.Period });
            }
            else
            {
                UnavailableDayPeriods[timeSlot.Day].Add(timeSlot.Period);
            }
        }

        public void Add(Curriculum curriculum)
        {
            Curricula.Add(curriculum);
        }

        public void Add(TimeSlot unavailableTimeSlot)
        {
            UnavailableTimeSlots.Add(unavailableTimeSlot);
        }
        public void Add(Room unsuitableRoom)
        {
            UnsuitableRooms.Add(unsuitableRoom);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var c = obj as Course;
            if (c == null) return false;
            return ID.CompareTo(c.ID) == 0;
        }
        public override string ToString()
        {
            return ID;
        }

        public override int GetHashCode()
        {
            return ID == null ? 0 : ID.GetHashCode();
        }

    }

    public class Curriculum
    {
        public string ID { get; private set; }
        public HashSet<Course> Courses { get; private set; }
        public int NumberOfCourses { get { return Courses.Count; } }

        public int NumberOfEstimatedStudents => Courses.ToList().Min(c => c.NumberOfStudents);

        public int Lectures { get { return Courses.Sum(c => c.Lectures); } }

        private HashSet<TimeSlot> _unavailableTimeSlots;
        public HashSet<TimeSlot> UnavailableTimeSlots
        {
            get
            {
                if (_unavailableTimeSlots == null)
                {
                    _unavailableTimeSlots = new HashSet<TimeSlot>(Courses.SelectMany(c => c.UnavailableTimeSlots));
                }
                return _unavailableTimeSlots;
            }
        }

        private Dictionary<int, HashSet<int>> _unavailableDayPeriods;
        public Dictionary<int, HashSet<int>> UnavailableDayPeriods
        {
            get
            {
                if (_unavailableDayPeriods == null)
                {
                    _unavailableDayPeriods = new Dictionary<int, HashSet<int>>();
                    foreach (var course in Courses)
                    {
                        foreach (var unPeriod in course.UnavailableDayPeriods)
                        {
                            if (!_unavailableDayPeriods.ContainsKey(unPeriod.Key))
                            {
                                _unavailableDayPeriods.Add(unPeriod.Key, new HashSet<int>(unPeriod.Value));
                            }
                            else
                            {
                                foreach (var element in unPeriod.Value)
                                {
                                    _unavailableDayPeriods[unPeriod.Key].Add(element);
                                }
                            }
                        }
                    }
                }
                return _unavailableDayPeriods;
            }
        }

        public Curriculum(string id)
        {
            ID = id;
            Courses = new HashSet<Course>();
        }

        public void AddCourse(Course c)
        {
            Courses.Add(c);
        }

        public override string ToString()
        {
            return ID;
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var q = obj as Curriculum;
            if (q == null) return false;
            return ID.CompareTo(q.ID) == 0;
        }

        public override int GetHashCode()
        {
            return ID == null ? 0 : ID.GetHashCode();
        }
    }

    public class Building
    {
        public string ID { get; private set; }

        public HashSet<Room> Rooms { get; private set; }

        public Building(string id)
        {
            ID = id;
            Rooms = new HashSet<Room>();
        }

        public override string ToString()
        {
            return ID;
        }
    }

    public class RoomCapacity
    {
        public string ID { get; private set; }
        public HashSet<Room> Rooms { get; private set; }

        public int Value { get; private set; }

        public RoomCapacity(IEnumerable<Room> rooms, int capacity)
        {
            ID = "Cap#" + capacity;
            Value = capacity;
            Rooms = new HashSet<Room>();
            foreach (var room in rooms)
            {
                if (room.Capacity == Value) Rooms.Add(room);
            }
        }
        public override string ToString()
        {
            return ID;
        }
    }

    public class Room
    {
        public string ID { get; private set; }
        public int Capacity { get; private set; }

        public int Cost => Capacity;//+(new Random(ID.GetHashCode()).Next(50)); 1

        public Building Building { get; private set; }

        public Room(string id, int capacity, Building building)
        {
            ID = id;
            Capacity = capacity;
            Building = building;
        }

        public Room(string id, int capacity)
        {
            ID = id;
            Capacity = capacity;
        }

        public override string ToString()
        {
            return ID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var r = obj as Room;
            if (r == null) return false;
            return ID.CompareTo(r.ID) == 0;
        }

        public override int GetHashCode()
        {
            return ID == null ? 0 : ID.GetHashCode();
        }
    }

    public class TimeSlot
    {
        public int Day { get; private set; }
        public int Period { get; private set; }
        public int Cost => (Day == 0 || Day == 6 ? 1 : 0 ) + (Period == 0 || Period >= 5 ? 1 : 0);
        
        public TimeSlot(int day, int period)
        {
            Day = day;
            Period = period;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var ts = obj as TimeSlot;
            if (ts == null) return false;
            return Day == ts.Day && Period == ts.Period;
        }

        public override int GetHashCode()
        {
            return Day ^ Period;
        }

        public override string ToString()
        {
            return string.Format("{0}_{1}", Day, Period);
        }

        public static bool TimeSlotsAreConsequtive(TimeSlot ts1, TimeSlot ts2)
        {
            if (ts1.Day != ts2.Day) return false;

            return ts1.Period == ts2.Period + 1 || ts1.Period + 1 == ts2.Period;
        }
    }
}
