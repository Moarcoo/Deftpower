using Deftpower.Onb.Assessment.Models;

// Replaced with request validation
namespace Deftpower.Onb.Assessment.Services
{
    // This session validator is not very well implemented and needs improvement.
    // Feel free to make adjustments or refactor the entire thing, it's fine to change its behavior as long as it validates charging sessions.
    class InspectSession : object
    {
        public void Start(ChargingSession? s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s), "Charging session cannot be null.");
            }

            Console.WriteLine("Verifying charging session" + s.SessionId + "for validaton.");

            List<string> forbiddenUsers = new List<string>();
            forbiddenUsers.Add("user-1");
            forbiddenUsers.Add("Q");

            for (int i = 1; i <= forbiddenUsers.ToList().Count(); i++)
            {
                if (forbiddenUsers.ElementAt(i - 1) == s.UserId)
                {
                    Console.WriteLine("User " + s.UserId + " is not allowed to start a charging session.");
                }
            }

            bool check = CheckSession(s);

            if (!check)
            {
                Console.WriteLine("Charging session" + s.SessionId + "is not valid.");
            }
        }

        bool CheckSession(ChargingSession? s)
        {
            bool valid = false;
            if (s == null)
            {
                valid = false;
            }
            else if (string.IsNullOrEmpty(s.UserId))
            {
                valid = false;
            }
            else if (s.SessionId == "")
            {
                valid = false;
            }
            else if (s.StartTime == new DateTime())
            {
                valid = false;
            }
            else
            {
                valid = true;
            }

            return valid;
        }
    }
}