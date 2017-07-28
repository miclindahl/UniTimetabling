using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Reflection;
using System.IO;

namespace UniversityTimetabling
{
    public enum ConstraintType
    {
        [XmlEnum("inactive")]
        InActive,
        [XmlEnum("hard")]
        Hard,
        [XmlEnum("soft")]
        Soft
    }

    [Serializable, XmlRoot("constraint")]
    public class Constraint
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public ConstraintType Type { get; set; }

        [XmlIgnore]
        public double Weight { get; set; }

        [XmlAttribute("weight")]
        public string weight
        {
            get { return Weight.ToString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Weight = double.Parse(value);
                }
            }
        }
    }

    public class Constraints
    {
        public string Name { get; set; }

        private Constraint _availability = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("availability")]
        public Constraint Availability
        {
            get { return _availability; }
            set { _availability = value; }
        }

        private Constraint _conflicts = new Constraint { Type = ConstraintType.Hard };

        [XmlElement("conflicts")]
        public Constraint Conflicts
        {
            get { return _conflicts; }
            set { _conflicts = value; }
        }

        private Constraint _doubleLectures = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("double_lectures")]
        public Constraint DoubleLectures
        {
            get { return _doubleLectures; }
            set { _doubleLectures = value; }
        }

        private Constraint _isolatedLectures = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("isolated_lectures")]
        public Constraint IsolatedLectures
        {
            get { return _isolatedLectures; }
            set { _isolatedLectures = value; }
        }

        private Constraint _lectures = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("lectures")]
        public Constraint Lectures
        {
            get { return _lectures; }
            set { _lectures = value; }
        }

        private Constraint _minWorkingDays = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("min_working_days")]
        public Constraint MinWorkingDays
        {
            get { return _minWorkingDays; }
            set { _minWorkingDays = value; }
        }

        private Constraint _roomCapacity = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("room_capacity")]
        public Constraint RoomCapacity
        {
            get { return _roomCapacity; }
            set { _roomCapacity = value; }
        }

        private Constraint _roomOccupancy = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("room_occupancy")]
        public Constraint RoomOccupancy
        {
            get { return _roomOccupancy; }
            set { _roomOccupancy = value; }
        }

        private Constraint _roomStability = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("room_stability")]
        public Constraint RoomStability
        {
            get { return _roomStability; }
            set { _roomStability = value; }
        }

        private Constraint _roomSuitability = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("room_suitability")]
        public Constraint RoomSuitability
        {
            get { return _roomSuitability; }
            set { _roomSuitability = value; }
        }

        private Constraint _studentMinMaxLoad = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("student_min_max_load")]
        public Constraint StudentMinMaxLoad
        {
            get { return _studentMinMaxLoad; }
            set { _studentMinMaxLoad = value; }
        }

        private Constraint _travelDistance = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("travel_distance")]
        public Constraint TravelDistance
        {
            get { return _travelDistance; }
            set { _travelDistance = value; }
        }

        private Constraint _windows = new Constraint { Type = ConstraintType.InActive };

        [XmlElement("windows")]
        public Constraint Windows
        {
            get { return _windows; }
            set { _windows = value; }
        }

        public static Constraints ReadXml(string path)
        {
            var constraints = new Constraints();

            var constrDict = typeof(Constraints)
                .GetProperties().Where(p => p.CanRead && p.GetValue(constraints, null) is Constraint)
                .ToDictionary(p => p.GetCustomAttribute<XmlElementAttribute>().ElementName,
                p => (Constraint)p.GetValue(constraints, null));

            var xDoc = XDocument.Load(path);

            constraints.Name = xDoc.Root.Attribute("name").Value;

            foreach (var node in xDoc.Root.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                var elem = (XElement)node;
                var constraint = GetXmlConstraint(elem);

                foreach (var p in typeof(Constraint).GetProperties())
                {
                    if (p.CanWrite)
                    {
                        var val = p.GetValue(constraint);
                        p.SetValue(constrDict[constraint.Name.ToLower()], val);
                    }
                }
            }

            return constraints;
        }

        private static Constraint GetXmlConstraint(XElement element)
        {
            var reader = new StringReader(element.ToString());
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Constraint));
            return (Constraint)xmlSerializer.Deserialize(reader);
        }

        public override string ToString()
        {
            var statsList = new List<Constraint>();

            foreach (PropertyInfo propertyInfo in typeof(Constraints).GetProperties())
            {
                if (!propertyInfo.CanRead) continue;

                var val = propertyInfo.GetValue(this);
                if (val.GetType() != typeof(Constraint)) continue;

                statsList.Add((Constraint)val);
            }

            statsList.Sort((t1, t2) =>
            {
                if (t1.Type == t2.Type) return t1.Name.CompareTo(t2.Name);
                if (t1.Type == ConstraintType.InActive) return -1;
                if (t2.Type == ConstraintType.InActive) return 1;
                if (t1.Type == ConstraintType.Hard) return -1;
                if (t2.Type == ConstraintType.Hard) return 1;

                return t1.Name.CompareTo(t2.Name);
            });
            var maxName = statsList.Max(ll => ll.Name.Length);

            var sb = new StringBuilder();
            sb.AppendFormat("Constraint setting \"{0}\":", Name).AppendLine();
            foreach (var constr in statsList)
            {
                var format = "    {0,-" + maxName + "}  ({1})";
                if (constr.Type == ConstraintType.Soft)
                {
                    format += "     : {2}";
                }

                sb.AppendFormat(format, constr.Name, constr.Type, constr.Weight);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string StatsToString(Dictionary<Constraint, double> stats)
        {
            var statsList = new List<Tuple<string, Constraint, double>>();

            foreach (PropertyInfo propertyInfo in typeof(Constraints).GetProperties())
            {
                if (propertyInfo.CanRead)
                {
                    var value = propertyInfo.GetValue(this, null);

                    if (!(value is Constraint)) continue;

                    var constr = (Constraint)value;
                    var name = "";
                    if (constr.Type == ConstraintType.Hard)
                    {
                        name = "Violations of ";
                    }
                    else if (constr.Type == ConstraintType.Soft)
                    {
                        name = "Cost of ";
                    }

                    name += constr.Name;

                    if (stats.ContainsKey(constr))
                    {
                        statsList.Add(Tuple.Create(name, constr, stats[constr]));
                    }
                    else
                    {
                        statsList.Add(Tuple.Create(name, constr, 0.0));
                    }
                }
            }

            statsList.Sort((t1, t2) =>
            {
                if (t1.Item2.Type == t2.Item2.Type) return t1.Item1.CompareTo(t2.Item1);
                if (t1.Item2.Type == ConstraintType.InActive) return -1;
                if (t2.Item2.Type == ConstraintType.InActive) return 1;
                if (t1.Item2.Type == ConstraintType.Hard) return -1;
                if (t2.Item2.Type == ConstraintType.Hard) return 1;

                return t1.Item1.CompareTo(t2.Item1);
            });
            var maxName = statsList.Max(ll => ll.Item1.Length);

            var sb = new StringBuilder();
            foreach (var tuple in statsList)
            {
                var format = "{0,-" + maxName + "}  ({1})";

                if (tuple.Item2.Type != ConstraintType.InActive)
                {
                    format += "     : {2}";
                }

                sb.AppendFormat(format, tuple.Item1, tuple.Item2.Type, tuple.Item3 * tuple.Item2.Weight);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
    public partial class Data
    {
        public static string GetStats(params string[] paths)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("File;Name;Courses;Lecturers;Rooms;Buildings;Curricula;Days;Periods_per_day;Constraints;Courses_with_less_periods");
            foreach (var path in paths)
            {
                var filename = new FileInfo(path).Name;

                var data = ReadXml(path, "3600", "1");
                sb.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}",
                    filename.Substring(0, filename.LastIndexOf('.')),
                    data.Name,
                    data.Courses.Count,
                    data.Lecturers.Count,
                    data.Rooms.Count,
                    data.Buildings.Count,
                    data.Curricula.Count,
                    data.Days,
                    data.Periods,
                    data.Courses.Sum(c => c.UnavailableTimeSlots.Count),
                    data.Courses.Count(c => c.Lectures < data.Periods)).AppendLine();
            }
            return sb.ToString();
        }

        public static Data ReadXml(string path, string timeLimitStr, string threadsString)
        {
            int timeLimit;
            if (!int.TryParse(timeLimitStr, out timeLimit))
            {
                throw new FormatException("The time limit given must be an integer");
            }
            int threads;
            if (!int.TryParse(threadsString, out threads))
            {
                throw new FormatException("The number of threads given must be an integer");
            }
            if (threads < 1)
            {
                threads = Environment.ProcessorCount;
            }
            var xDoc = XDocument.Load(path);
            var instanceName = xDoc.Root.Attribute("name").Value;

            int days;
            int periods_per_days;
            int min_daily_lectures;
            int max_daily_lectures;

            GetXmlDescriptor(xDoc.Root.Element("descriptor"), out days, out periods_per_days, out min_daily_lectures, out max_daily_lectures);
            var timeSlots = new Dictionary<int, Dictionary<int, TimeSlot>>();

            for (int d = 0; d < days; d++)
            {
                timeSlots.Add(d, new Dictionary<int, TimeSlot>());
                for (int t = 0; t < periods_per_days; t++)
                {
                    timeSlots[d].Add(t, new TimeSlot(d, t));
                }
            }

            var courses = GetXmlCourses(xDoc.Root.Element("courses"));
            var rooms = GetXmlRooms(xDoc.Root.Element("rooms"));
            var curricula = GetXmlCurricula(xDoc.Root.Element("curricula"), courses);
            GetXmlConstraints(xDoc.Root.Element("constraints"), courses, rooms, timeSlots);

            return new Data(courses.Values, courses.Select(c => c.Value.Lecturer).Distinct(), rooms.Values, curricula,
                timeSlots.SelectMany(tt => tt.Value.Values), days, periods_per_days, min_daily_lectures,
                max_daily_lectures, timeLimit) { Name = instanceName, Filename = Path.GetFileNameWithoutExtension(path), Threads = threads };
        }

        private static void GetXmlDescriptor(XElement element, out int days, out int periods_per_day, out int min_daily_lectures, out int max_daily_lectures)
        {
            days = int.Parse(element.Element("days").Attribute("value").Value);
            periods_per_day = int.Parse(element.Element("periods_per_day").Attribute("value").Value);
            var daily_lectures = element.Element("daily_lectures");
            min_daily_lectures = int.Parse(daily_lectures.Attribute("min").Value);
            max_daily_lectures = int.Parse(daily_lectures.Attribute("max").Value);
        }

        private static Dictionary<string, Course> GetXmlCourses(XElement element)
        {
            var courses = new Dictionary<string, Course>();
            var lecturers = new Dictionary<string, Lecturer>();
            foreach (var node in element.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                var elem = (XElement)node;
                if (elem.Name.ToString().ToLower() != "course") continue;

                var id = elem.Attribute("id").Value;
                var lecturerStr = elem.Attribute("teacher").Value;
                var lectures = int.Parse(elem.Attribute("lectures").Value);
                var min_days = int.Parse(elem.Attribute("min_days").Value);
                var students = int.Parse(elem.Attribute("students").Value);
                var double_lectures = elem.Attribute("double_lectures").Value.ToLower() == "yes";

                if (!lecturers.ContainsKey(lecturerStr))
                {
                    lecturers.Add(lecturerStr, new Lecturer(lecturerStr));
                }

                var course = new Course(
                    id, lecturers[lecturerStr], lectures, min_days, students, double_lectures);

                lecturers[lecturerStr].Add(course);

                courses.Add(id, course);
            }
            return courses;
        }

        private static Dictionary<string, Room> GetXmlRooms(XElement element)
        {
            var rooms = new Dictionary<string, Room>();
            var buildings = new Dictionary<string, Building>();
            foreach (var node in element.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                var elem = (XElement)node;
                if (elem.Name.ToString().ToLower() != "room") continue;

                var id = elem.Attribute("id").Value;
                var cap = int.Parse(elem.Attribute("size").Value);
                var buildingStr = elem.Attribute("building").Value;

                if (!buildings.ContainsKey(buildingStr))
                {
                    buildings.Add(buildingStr, new Building(buildingStr));
                }

                var room = new Room(id, cap, buildings[buildingStr]);
                buildings[buildingStr].Rooms.Add(room);
                rooms.Add(id, room);
            }
            return rooms;
        }

        private static HashSet<Curriculum> GetXmlCurricula(XElement element, Dictionary<string, Course> courses)
        {
            var curricula = new HashSet<Curriculum>();

            foreach (var node in element.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                var elem = (XElement)node;
                if (elem.Name.ToString().ToLower() != "curriculum") continue;

                var id = elem.Attribute("id").Value;

                var cur = new Curriculum(id);
                foreach (var xNode in elem.DescendantNodes())
                {
                    if (xNode.NodeType != XmlNodeType.Element) continue;

                    var courseelem = (XElement)xNode;
                    if (courseelem.Name.ToString().ToLower() != "course")
                    {
                        continue;
                    }

                    var courseid = courseelem.Attribute("ref").Value;

                    cur.AddCourse(courses[courseid]);
                    courses[courseid].Add(cur);
                }
                curricula.Add(cur);
            }

            return curricula;
        }

        private static void GetXmlConstraints(XElement element, Dictionary<string, Course> courses,
            Dictionary<string, Room> rooms, Dictionary<int, Dictionary<int, TimeSlot>> timeSlots)
        {
            foreach (var node in element.DescendantNodes())
            {
                if (node.NodeType != XmlNodeType.Element) continue;

                var elem = (XElement)node;
                if (elem.Name.ToString().ToLower() != "constraint") continue;

                var type = elem.Attribute("type").Value.ToLower();
                var course = courses[elem.Attribute("course").Value];

                if (type == "period")
                {
                    foreach (var xnode in elem.DescendantNodes())
                    {
                        if (xnode.NodeType != XmlNodeType.Element) continue;

                        var xelem = (XElement)xnode;
                        if (xelem.Name.ToString().ToLower() != "timeslot") continue;

                        var day = int.Parse(xelem.Attribute("day").Value);
                        var period = int.Parse(xelem.Attribute("period").Value);

                        course.Add(timeSlots[day][period]);
                    }
                }
                else if (type == "room")
                {
                    foreach (var xnode in elem.DescendantNodes())
                    {
                        if (xnode.NodeType != XmlNodeType.Element) continue;

                        var xelem = (XElement)xnode;
                        if (xelem.Name.ToString().ToLower() != "room") continue;

                        var roomId = xelem.Attribute("ref").Value;

                        course.Add(rooms[roomId]);
                    }
                }
            }
        }


        public bool CurriculumPatternPenalty(Constraints constraints, List<int> pattern, out double cost)
        {
            cost = 0.0;
            var result = true;
            for (int i = 0; i < pattern.Count; i++)
            {
                if ((i == 0 || pattern[i - 1] + 1 < pattern[i]) &&
                    (i == pattern.Count - 1 || pattern[i + 1] - 1 > pattern[i]))
                {
                    if (constraints.IsolatedLectures.Type == ConstraintType.Soft)
                    {
                        cost += constraints.IsolatedLectures.Weight;
                    }
                    else if (constraints.IsolatedLectures.Type == ConstraintType.Hard)
                    {
                        result = false;
                    }
                }
                if (i > 0 && pattern[i] > pattern[i - 1] + 1)
                {
                    if (constraints.Windows.Type == ConstraintType.Soft)
                    {
                        cost += (pattern[i] - pattern[i - 1] - 1) * constraints.Windows.Weight;
                    }
                    else if (constraints.Windows.Type == ConstraintType.Hard)
                    {
                        result = false;
                    }
                }
            }

            if (pattern.Count > 0 && constraints.StudentMinMaxLoad.Type != ConstraintType.InActive)
            {
                var diff = MinimumPeriodsPerDay > pattern.Count
                    ? MinimumPeriodsPerDay - pattern.Count
                    : MaximumPeriodsPerDay < pattern.Count
                        ? pattern.Count - MaximumPeriodsPerDay
                        : 0;
                if (diff > 0)
                {
                    if (constraints.StudentMinMaxLoad.Type == ConstraintType.Hard)
                    {
                        result = false;
                    }
                    else if (constraints.StudentMinMaxLoad.Type == ConstraintType.Soft)
                    {
                        cost += constraints.StudentMinMaxLoad.Weight * diff;
                    }
                }
            }

            return result;
        }

    }
}
