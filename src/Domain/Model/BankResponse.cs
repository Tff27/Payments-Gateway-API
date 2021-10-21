namespace Domain.Model
{
    public class BankResponse
    {
        public string ErrorMessage { get; set; }

        public bool IsSuccess => ErrorMessage == null;
    }
}
