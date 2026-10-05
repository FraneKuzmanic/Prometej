# A post outlives its author's account

Every period has a discussion: topics, and under each topic a flat list of replies. It is the
first text one user writes for others to read, so deleting an account stopped being a private
matter. Until now an account took its own data with it. A student's stored plays are deleted
with the student, and a teacher's account cannot be deleted while it still has quizzes, because
other people's results hang on them.

A post is different from a play. A topic holds what other people answered, and a reply is half
of a conversation: remove it and the replies after it answer nothing.

So a post stays when its author's account is deleted. The author column of a topic and of a
reply is nullable and the database sets it to null when the account goes. The post is then
shown as written by "Obrisani korisnik" (a deleted user) and belongs to nobody.

The same reasoning limits what an author may delete while the account exists. A reply can
always be deleted by its author. A topic can be deleted by its author only while it has no
replies; after that only an admin can delete it, together with what was answered under it.

## Considered options

- **Delete the posts with the account**, as plays are. A student who leaves would take the
  answers of everyone who replied to their topics.
- **Refuse to delete an account that has posts**, as an account with quizzes is refused. A
  quiz can be deleted by its creator to clear the way; a topic with replies cannot, so the
  account could never be deleted without an admin.
- **Keep a copy of the author's name on the post.** The name would survive the account, which
  is the opposite of what deleting an account should mean, and every change of name would
  have to rewrite the copies. A stored play has such a copy, and pays for it that way.
- **Hand orphaned posts to a placeholder account.** A row in the users table that is not a
  user, to avoid one nullable column.

## Consequences

- The text a person wrote stays public after they delete their account, without their name.
  Only an admin can remove it then.
- The name and the role shown on a post are read from the account as it is now. Nothing is
  copied, so a changed name shows on old posts at once.
- A post's author can be missing, and every read has to handle that.
- Who may delete a post is decided by the server and sent with it as a yes or no. The
  response carries no account id, and the client holds no copy of the rule.
- "No replies" is checked and then the topic is deleted, in two steps. A reply arriving in
  between is deleted with the topic. The window is a few milliseconds and is accepted.
