namespace Library_API.Entity
{
    public class User
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string MobileNumber {  get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }

        public UserType UserType { get; set; } = UserType.NONE;

        public AccountStatus AccountStatus { get; set; } = AccountStatus.UNAPROOVED;
    }

    public enum UserType
    {
        NONE ,ADMIN, STUDENT
    }
    public enum AccountStatus
    {
        UNAPROOVED , ACTIVE , BLOCKED
    }
}
