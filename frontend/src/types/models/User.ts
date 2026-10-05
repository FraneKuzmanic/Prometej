import ROLE from "../enums/Role";

export interface LoginInput {
    email: string;
    password: string;
}

export interface RegisterInput {
    email: string;
    firstName: string;
    lastName: string;
    password: string;
    repeatedPassword: string;
}

export interface UserCreateRequest{
    firstName: string;
    lastName: string;
    email: string;
    password: string;
}

export interface UserViewModel{
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    role: ROLE;
}

export interface UserNameEditRequest{
    firstName: string;
    lastName: string;
}

export interface UserPasswordEditRequest{
    currentPassword: string;
    newPassword: string;
}

export interface PasswordInput extends UserPasswordEditRequest{
    repeatedPassword: string;
}

// a row of the Admin's list of Users
export interface UserAccount extends UserViewModel{
    quizCount: number;
}
