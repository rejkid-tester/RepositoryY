namespace Backend.Responses
{
    public class GetTasksResponse : BaseResponse
    {
        public List<LocalTask>? Tasks { get; set; }
    }
}
