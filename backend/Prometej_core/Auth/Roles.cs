namespace Prometej_core.Auth
{
    public static class Roles
    {
        public const string Student = "student";
        public const string Teacher = "teacher";
        public const string Admin = "admin";

        // For [Authorize(Roles = ...)], where a comma means "any of".
        public const string TeacherOrAdmin = Teacher + "," + Admin;

        public static readonly string[] All = [Student, Teacher, Admin];
    }
}
