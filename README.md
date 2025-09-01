<h2>Prometej - a web platform that helps students learn Croatian literature</h2>

<p>This project was developed for my college final work. The idea was to colaborate with my high school professors who would use the platform to write quality learning material for students. Prometej is an interactive learning application designed to help students with learning Croatian literature. It provides written learning materials curated by administrators and allows students to practice their knowledge through quizzes created by teachers. Quizzes can be public or private, enabling tailored assessment for specific classes or groups. The system aims to support teachers in sharing knowledge efficiently while motivating students through a dynamic digital learning environment. </p>

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
 cd prometej_frontend  </pre>
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
<pre lang="markdown">cd Prometej_backend</pre>
<p>3. Restore dependencies</p>
<pre lang="markdown">dotnet restore</pre>
<p>4. In <b>appsetings.json</b> file configure the connection string for postgreSQL database</p>
<p>5. Run code first migrations</p>
<pre lang="markdown">dotnet ef database update</pre>
<p>6. Start the server side</p>
<pre lang="markdown">dotnet run</pre>

