namespace RDCS.EmployeeAgent.Runtime.Screenshot.Events;

public record CleanupCompleted(
    int DeletedCount,
    long FreedSpaceBytes,
    DateTime Timestamp
);
