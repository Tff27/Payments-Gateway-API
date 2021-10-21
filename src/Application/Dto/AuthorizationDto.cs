namespace Application.Dto
{
    public class AuthorizationDto
    {
        public string Number { get; set; }

        public int ExpirationMonth { get; set; }

        public int ExpirationYear { get; set; }

        public int Cvv { get; set; }

        public double Amount { get; set; }

        public string Currency { get; set; }
    }
}
