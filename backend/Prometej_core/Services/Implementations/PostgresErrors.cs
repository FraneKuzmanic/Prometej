using Npgsql;

namespace Prometej_core.Services.Implementations
{
    internal static class PostgresErrors
    {
        // Two saves that each wait for a row the other holds: the database stops one of them.
        // The provider reports it wrapped in more than one exception.
        public static bool IsDeadlock(Exception? exception)
        {
            for (; exception != null; exception = exception.InnerException)
            {
                if (exception is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected })
                {
                    return true;
                }
            }

            return false;
        }
    }
}
