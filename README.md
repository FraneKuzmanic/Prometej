<h2>Prometej - a web platform that helps students learn Croatian literature</h2>

<p>This project was developed for my college final work. The idea was to colaborate with my high school professors who would use the platform to write quality learning material for students. It is an interactive learning application designed to help students with learning Croatian literature. It provides written learning materials curated by administrators and allows students to practice their knowledge through quizzes created by teachers. Quizzes can be public or private, enabling tailored assessment for specific classes or groups. The system aims to support teachers in sharing knowledge efficiently while motivating students through a dynamic digital learning environment. </p>

<p>If you want to check out full paper written in Croatian with application screenshots you can see it here <a href="https://drive.google.com/file/d/1dKCoy6_ElFA_kVEj95mHxvKKT-1d5zTl/view?usp=drive_link" target="_blank">Prometej</a></p>

<p>Technologies used:</p>
<ul>
  <li><b>React</b> – front-end library for building user interfaces</li>
  <li><b>TypeScript</b> – strongly typed language that extends JavaScript</li>
  <li><b>.NET</b> – back-end framework for building APIs and business logic</li>
  <li><b>PostgreSQL</b> – relational database for storing and managing data</li>
</ul>

<h3>Installation:</h3>

<h4>Client-side:</h4>
<p>1. Check if you have <a href="https://nodejs.org/" target="_blank">Node.js</a>, if not download it</p>
<p>2. Clone the repository</p>
<pre lang="markdown"> git clone https://github.com/FraneKuzmanic/Prometej.git
 cd Prometej/frontend  </pre>
 <p>3. Install  dependencies</p>
 <pre lang="markdown"> npm install
 # or
 yarn install </pre>
 <p>4. Start the development server</p>
  <pre lang="markdown">npm run dev
# or
yarn dev </pre>

<h4>Server-side:</h4> 
<p>1. Check if you have <a href="https://dotnet.microsoft.com/en-us/download/dotnet/8.0" target=_blank">.NET 8</a>, if not download it</p>
<p>2. Navigate to a repository</p>
<pre lang="markdown">cd backend</pre>
<p>3. Restore dependencies</p>
<pre lang="markdown">dotnet restore</pre>
<p>4. In <b>Prometej_api/appsettings.json</b> configure the connection string for the PostgreSQL database</p>
<p>5. Set the secrets the server needs: a signing key for the session token (base64 of at least 32 random bytes, for example the output of <code>openssl rand -base64 48</code>) and the first admin account. Registration through the application only creates students, so teacher and admin accounts are created from this configuration when the server starts.</p>
<pre lang="markdown">dotnet user-secrets set "Jwt:Key" "&lt;base64 key&gt;" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Email" "admin@example.com" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Password" "&lt;password&gt;" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:FirstName" "Admin" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:LastName" "Prometej" --project Prometej_api
dotnet user-secrets set "Seed:Users:0:Role" "admin" --project Prometej_api</pre>
<p>Repeat with index <code>1</code> and role <code>teacher</code> for a teacher account.</p>
<p>6. Start the server. In development it applies the database migrations on start.</p>
<pre lang="markdown">dotnet run --project Prometej_api --launch-profile https</pre>
<p>The client development server forwards <code>/api</code> to <code>https://localhost:7041</code>, so start the server before opening the client.</p>

<h4>Tests:</h4>
<p>The server has integration tests that run the API against PostgreSQL in a container, so Docker has to be running.</p>
<pre lang="markdown">cd backend
dotnet test</pre>

