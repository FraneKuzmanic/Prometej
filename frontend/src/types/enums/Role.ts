enum ROLE {
  Student = "student",
  Teacher = "teacher",
  Admin = "admin",
}

// what a Role is called on screen
export const roleLabels: Record<ROLE, string> = {
  [ROLE.Student]: "Učenik",
  [ROLE.Teacher]: "Nastavnik",
  [ROLE.Admin]: "Administrator",
};

export default ROLE;