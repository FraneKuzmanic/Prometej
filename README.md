# Prometej

A web platform for learning Croatian literature. The learning material is organised by
literary period; quizzes let students check what they read and let teachers see how a class did.

It started in 2024 as my final paper at university, written with the idea that my high-school
teachers would author the material. The [paper](https://drive.google.com/file/d/1dKCoy6_ElFA_kVEj95mHxvKKT-1d5zTl/view?usp=drive_link)
(in Croatian, with screenshots) describes that first version. Since then the project has been
reworked: real authentication, validated quizzes, stored results, search, and the twelve
periods of the national exam catalogue.

## What it does

**Anyone**, without an account

- reads the material of a period, with a contents list that follows the reading, and searches
  it; the search ignores case and Croatian diacritics, so "senoa" finds "Šenoa"
- plays public quizzes, or a private one with the five-digit entry code a teacher gave the class
- answers three kinds of question: four options, matching pairs, and putting items in order,
  with a point for every right pair or place
- reads the poem or excerpt a question is asked about beside the question, the form most of
  the national exam's reading and literature tasks take
- can open a hint before answering and, where the quiz has one, reads an explanation after it
- reads the discussion of a period: topics, and the replies under each

**Anyone signed in** (registering creates a student account)

- has every play stored, and sees it again under "Moji rezultati", answer by answer
- sees their progress per period
- opens a topic in a period's discussion or replies to one; a teacher's and an admin's post
  is marked as such
- deletes their own reply, and their own topic while nobody has replied to it
- changes their name and password

**A teacher**

- writes quizzes, public or private, optionally tied to a period, mixing four-option, matching
  and ordering questions, and can put a source text above a run of questions
- sees how a quiz was played: every play and its answers, each question with how often it was
  answered right and the wrong answer chosen most often (pair by pair and place by place for
  the other two kinds), and each student's number of plays, first and best result

**An admin**

- writes the material of each period in a rich-text editor
- can write quizzes like a teacher, and edit, delete or read the results of any teacher's quiz
- makes a student a teacher or an admin, and back, from a list of the accounts
- removes any topic or reply from a discussion

The interface is in Croatian.

## How it is built

```
backend/     .NET 8, ASP.NET Core, EF Core, PostgreSQL
  Prometej_api/            host, controllers, authentication, seed content
  Prometej_core/           entities, request and view models, services
  Prometej_persistance/    DbContext, migrations, repository
  Prometej_tests/          integration tests
frontend/    React 18, TypeScript, Vite, Redux Toolkit, MUI
```

A few things worth knowing before reading the code:

- **The session is a JWT in an `HttpOnly`, `Secure`, `SameSite=Lax` cookie.** The page's script
  never sees it. The client calls a relative `/api`, which the dev server proxies to the API,
  so the browser sees one origin. The token only says who is calling: their role, and whether
  the session still holds, are read from the account on every request
  ([0004](docs/adr/0004-a-session-is-checked-against-the-stored-account.md)).
- **The server trusts nothing it can compute.** Who is calling comes from the token and their
  role from the database. A play is submitted as numbers (the option chosen, the pairs made,
  the order given); the server builds the result from the stored questions and ignores a score
  in the request.
- **Old results never change.** An answer keeps the texts it was played with, and a question
  removed from a played quiz is retired instead of deleted, so editing a quiz does not rescore
  the plays before the edit. A source text that was played is kept the same way: an edit
  stores a new version, and the review of an old play shows the one that was read.
- **A point is an answer row.** A play stores a row for each point it could win: one for a
  four-option question, one for each pair, one for each place. The score is the number of
  right rows, as it was when every question was worth one, so no stored result was migrated
  ([0005](docs/adr/0005-one-answer-row-per-point.md)).
- **Errors are typed.** Services throw `NotFoundException`, `ForbiddenException` and the like;
  one handler maps them to status codes. Controllers have no `try/catch`.
- **A retried submit stores one play.** The client sends a key per play, and a unique index
  is the guarantee.
- **A post outlives its author's account.** A topic holds what other people answered, so
  deleting an account leaves its posts in place without a name, and an author can delete a
  topic only while it has no replies
  ([0006](docs/adr/0006-a-post-outlives-its-authors-account.md)). Who may delete a post is
  the server's answer, sent with the post. One account can post five times a minute.

Decisions with a longer story are in [`docs/adr`](docs/adr):

- [0001](docs/adr/0001-retire-played-questions.md): a question removed from a played quiz is retired, not deleted
- [0002](docs/adr/0002-correct-answer-as-option-number.md): the correct answer is stored as an option number
- [0003](docs/adr/0003-period-ids-are-not-their-order.md): a period's id is not its place in the curriculum order
- [0004](docs/adr/0004-a-session-is-checked-against-the-stored-account.md): a session is checked against the stored account
- [0005](docs/adr/0005-one-answer-row-per-point.md): a point is an answer row, whatever the type of question
- [0006](docs/adr/0006-a-post-outlives-its-authors-account.md): a post in a discussion outlives its author's account

## Running it locally

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0),
[Node.js](https://nodejs.org/) and PostgreSQL. Docker is needed only for the tests.

### 1. Configuration

No secret is kept in a tracked file. From `backend/`, set the connection string, a signing key
for the session token (base64 of at least 32 random bytes, for example the output of
`openssl rand -base64 48`) and the first admin account:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Username=postgres;Password=<password>;Database=prometej" --project Prometej_api
dotnet user-secrets set "Jwt:Key" "<base64 key>" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Email" "admin@example.com" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Password" "<password>" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:FirstName" "Admin" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:LastName" "Prometej" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Role" "admin" --project Prometej_api
```

Registration only creates students. An admin can make any of them a teacher under "Korisnici";
a seeded teacher account is a shortcut: repeat the five `Seed:Users` lines with index `1` and
role `teacher`.

### 2. The server

```
cd backend
dotnet run --project Prometej_api --launch-profile https
```

In development the server creates the database if it is missing, applies the migrations,
creates the seeded accounts and fills the periods with sample content and three quizzes. The
database user therefore has to be allowed to create a database and the `unaccent` extension.
The server listens on `https://localhost:7041`.

### 3. The client

```
cd frontend
npm install
npm run dev
```

It serves `http://localhost:5173` and forwards `/api` to the server, so start the server first.
`VITE_PROXY_TARGET` in a `.env` file changes the target (see `.env.example`).

## Tests

```
cd backend
dotnet test
```

The tests are integration tests: they start the real API against PostgreSQL in a container
and talk to it over HTTP, so Docker has to be running. They cover authentication and
authorization per role, quiz validation, the three question types and source texts, plays and
results, both searches, the discussion, and the migrations (a migration is run against rows of the older
schema, to show what it does to them).

The client has no automated tests yet; `npm run lint` and `npm run build` are its gates.

## Sample content

Every period has study material in the same seven chapters: the name and time frame, the
social and historical context, the features of the period, European literature, Croatian
literature, the works, and a summary. Under "the works", the two to five works a student has to
know are taken one by one: a note on the writer, the literary elements, a short summary and the
characters. Three periods also have a sample quiz of thirteen questions: three on a poem, or
an excerpt of one, shown beside them, eight four-option questions, one matching and one
ordering question. The poems (by Hanibal Lucić, Silvije Strahimir Kranjčević and Antun Gustav
Matoš) are in the public domain and are quoted from Croatian Wikisource.

The choice of works follows the NCVVO exam catalogue. The facts were taken from Hrvatska
enciklopedija, lektire.hr, Croatian Wikipedia and Leksikon Marina Držića; the text was written
for this project. It is sample material and **has not been reviewed by a teacher**.

## Not done yet

- The application is not deployed.
- No password reset: a forgotten password cannot be recovered.
- No automated tests for the client.
- A private quiz is practice, not a test: the correct answer is shown after each question.
- In a discussion a post cannot be edited or reported; an admin deleting it is the only
  remedy, and nothing updates live.
