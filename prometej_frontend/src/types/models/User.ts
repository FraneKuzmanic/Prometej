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
    role: ROLE;
}

export interface UserViewModel{
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    role: ROLE;
    token?: string;
}

export interface User {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    role: ROLE;
}
