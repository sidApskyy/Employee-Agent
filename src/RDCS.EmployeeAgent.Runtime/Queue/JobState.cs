namespace RDCS.EmployeeAgent.Runtime.Queue;

public enum JobState
{
    Pending,
    Scheduled,
    Running,
    Completed,
    Failed,
    Retrying,
    DeadLetter
}
