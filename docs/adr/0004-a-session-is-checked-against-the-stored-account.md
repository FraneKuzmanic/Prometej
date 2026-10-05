# A session is checked against the stored account

The session is a signed token in a cookie, valid for eight hours. A signed token stays valid
whatever happens to its account, and three things can happen to it: the account is deleted, an
admin changes its role, or its password is changed. A token that carried the role would keep
a demoted teacher a teacher, and a session opened on another device would survive a password
change, both for up to eight hours.

So the token only says who is calling. Each authenticated request reads the account by its
primary key, and that one query decides the rest: a missing account ends the session, the role
the request runs with is the stored one, and a session stamp kept on the account has to equal
the one in the token. A password change replaces the stamp and answers with a new cookie.

## Considered options

- **Trust the token until it expires.** No query, and a role change or a password change that
  takes up to eight hours to hold.
- **Short-lived access tokens with a refresh token.** The usual answer at scale. It adds a
  second token, a rotation endpoint and a store of refresh tokens, to save one indexed read
  per request.
- **Server-side sessions.** The same query per request, and a session table to manage as well.
- **A deny-list of revoked tokens.** A table that has to be read on every request anyway.

## Consequences

- Every authenticated request costs one query. The check for a deleted account already cost it.
- The token is an identity, not a permission. It carries no role.
- A role change holds at the next request and signs nobody out.
- A password change signs out every other session; the browser that made the change gets a
  new cookie and stays signed in.
- Accounts that existed before the stamp start with the empty one, which is as good as any
  until their password changes. A token issued before the stamp existed carries none and is
  rejected, so such a session has to sign in once more.
- An admin cannot change their own role, so nobody loses it by a slip. Nothing stops the only
  admin from deleting their own account.
