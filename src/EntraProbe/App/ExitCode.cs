namespace EntraProbe.App;

public enum ExitCode
{
    Success = 0,
    DepartmentMissing = 10,
    UnsupportedExecutionContext = 20,
    InvalidConfiguration = 30,
    AuthenticationFailure = 40,
    GraphFailure = 50,
    UnexpectedError = 99
}
