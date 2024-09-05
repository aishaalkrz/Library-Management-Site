
using Library_API.Context;
using Library_API.Entity;
using Library_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        public DbContextBook contextBook { get; }
        public JwtService JwtService { get; }

        public EmailService EmailService { get; }

        public LibraryController(DbContextBook contextBook, JwtService jwtService , EmailService emailService)
        {
            this.contextBook = contextBook;
          
            JwtService = jwtService;
            EmailService = emailService;

        }
        [HttpPost("Register")]
        public ActionResult Register(User user)
        {
            user.AccountStatus = AccountStatus.UNAPROOVED;
            user.UserType = UserType.STUDENT;
            user.CreatedOn = DateTime.Now;

            contextBook.Users.Add(user);
            contextBook.SaveChanges();

            const string subject = "Account Created";
            var body = $"""
                <html>
                    <body>
                        <h1>Hello, {user.FirstName} {user.LastName}</h1>
                        <h2>
                            Your account has been created and we have sent approval request to admin. 
                            Once the request is approved by admin you will receive email, and you will be
                            able to login in to your account.
                        </h2>
                        <h3>Thanks</h3>
                    </body>
                </html>
            """;

            EmailService.SendEmail(user.Email, subject, body);

            return Ok(@"Thank you for registering. 
                        Your account has been sent for aprooval. 
                        Once it is aprooved, you will get an email.");
        }

        [HttpGet("Login")]
        public ActionResult Login(string email, string password)
        {
            if (contextBook.Users.Any(u => u.Email.Equals(email) && u.Password.Equals(password)))
            {
                var user = contextBook.Users.Single(user => user.Email.Equals(email) && user.Password.Equals(password));

                if (user.AccountStatus == AccountStatus.UNAPROOVED)
                {
                    return Ok("unapproved");
                }

                if (user.AccountStatus == AccountStatus.BLOCKED)
                {
                    return Ok("blocked");
                }

                return Ok(JwtService.GenerateToken(user));
            }
            return Ok("not found");
        }

        [Authorize]
        [HttpGet("GetBooks")]
        public ActionResult GetBooks()
        {
            if (contextBook.Books.Any())
            {
                return Ok(contextBook.Books.Include(b => b.BookCategory).ToList());
            }
            return NotFound();
        }

        [Authorize]
        [HttpPost("OrderBook")]
        public ActionResult OrderBook(int userId, int bookId)
        {
            var canOrder = contextBook.Orders.Count(o => o.UserId == userId && !o.Returned) < 3;

            if (canOrder)
            {
                contextBook.Orders.Add(new()
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    Returned = false,
                    ReturnDate = null,
                    FinePaid = 0
                });

                var book = contextBook.Books.Find(bookId);
                if (book is not null)
                {
                    book.Ordered = true;
                }


                contextBook.SaveChanges();
                return Ok("ordered");
            }

            return Ok("cannot order");
        }

        [Authorize]
        [HttpGet("GetOrdersOFUser")]
        public ActionResult GetOrdersOFUser(int userId)
        {
            var orders = contextBook.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToList();
            if (orders.Any())
            {
                return Ok(orders);
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost("AddCategory")]
        [Authorize]
        public ActionResult AddCategory(BookCategory bookCategory)
        {
            var exists = contextBook.BookCategories.Any(bc => bc.Category == bookCategory.Category && bc.SubCategory == bookCategory.SubCategory);
            if (exists)
            {
                return Ok("cannot insert");
            }
            else
            {
                contextBook.BookCategories.Add(new()
                {
                    Category = bookCategory.Category,
                    SubCategory = bookCategory.SubCategory
                });
                contextBook.SaveChanges();
                return Ok("INSERTED");
            }
        }
        [Authorize]
        [HttpGet("GetCategories")]
        public ActionResult GetCategories()
        {
            var categories = contextBook.BookCategories.ToList();
            if (categories.Any())
            {
                return Ok(categories);
            }
            return NotFound();
        }

        [Authorize]
        [HttpPost("AddBook")]
        public ActionResult AddBook(Book book)
        {
            book.BookCategory = null;
            contextBook.Books.Add(book);
            contextBook.SaveChanges();
            return Ok("inserted");
        }

        [Authorize]
        [HttpDelete("DeleteBook")]
        public ActionResult DeleteBook(int id)
        {
            var exists = contextBook.Books.Any(b => b.Id == id);
            if (exists)
            {
                var book = contextBook.Books.Find(id);
                contextBook.Books.Remove(book!);
                contextBook.SaveChanges();
                return Ok("deleted");
            }
            return NotFound();
        }

        [HttpGet("ReturnBook")]
        public ActionResult ReturnBook(int userId, int bookId, int fine)
        {
            var order = contextBook.Orders.SingleOrDefault(o => o.UserId == userId && o.BookId == bookId);
            if (order is not null)
            {
                order.Returned = true;
                order.ReturnDate = DateTime.Now;
                order.FinePaid = fine;

                var book = contextBook.Books.Single(b => b.Id == order.BookId);
                book.Ordered = false;

                contextBook.SaveChanges();

                return Ok("returned");
            }
            return Ok("not returned");
        }

        [Authorize]
        [HttpGet("GetUsers")]
        public ActionResult GetUsers()
        {
            return Ok(contextBook.Users.ToList());
        }

        [Authorize]
        [HttpGet("ApproveRequest")]
        public ActionResult ApproveRequest(int userId)
        {
            var user = contextBook.Users.Find(userId);

            if (user is not null)
            {
                if (user.AccountStatus == AccountStatus.UNAPROOVED)
                {
                    user.AccountStatus = AccountStatus.ACTIVE;
                    contextBook.SaveChanges();

           
                    return Ok("approved");
                }
            }

            return Ok("not approved");
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public ActionResult GetOrders()
        {
            var orders = contextBook.Orders.Include(o => o.User).Include(o => o.Book).ToList();
            if (orders.Any())
            {
                return Ok(orders);
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize]
        [HttpGet("SendEmailForPendingReturns")]
        public ActionResult SendEmailForPendingReturns()
        {
            var orders = contextBook.Orders
                            .Include(o => o.Book)
                            .Include(o => o.User)
                            .Where(o => !o.Returned)
                            .ToList();

            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

            var firstFineEmails = emailsWithFine.Where(x => x.FinePaid == 50).ToList();
            firstFineEmails.ForEach(x =>
            {
                var body = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>Yesterday was your last day to return Book: "{x.Book?.Title}".</h4>
                        <h4>From today, every day a fine of 50Rs will be added for this book.</h4>
                        <h4>Please return it as soon as possible.</h4>
                        <h4>If your fine exceeds 500Rs, your account will be blocked.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                EmailService.SendEmail(x.User!.Email, "Return Overdue", body);
            });

            var regularFineEmails = emailsWithFine.Where(x => x.FinePaid > 50 && x.FinePaid <= 500).ToList();
            regularFineEmails.ForEach(x =>
            {
                var regularFineEmailsBody = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Pleae pay it as soon as possible.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                EmailService.SendEmail(x.User?.Email!, "Fine To Pay", regularFineEmailsBody);
            });

            var overdueFineEmails = emailsWithFine.Where(x => x.FinePaid > 500).ToList();
            overdueFineEmails.ForEach(x =>
            {
                var overdueFineEmailsBody = $"""
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Your account is BLOCKED.</h4>
                        <h4>Pleae pay it as soon as possible to UNBLOCK your account.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                EmailService.SendEmail(x.User?.Email!, "Fine Overdue", overdueFineEmailsBody);
            });

            return Ok("sent");
        }

        [Authorize]
        [HttpGet("BlockFineOverdueUsers")]
        public ActionResult BlockFineOverdueUsers()
        {
            var orders = contextBook.Orders
                            .Include(o => o.Book)
                            .Include(o => o.User)
                            .Where(o => !o.Returned)
                            .ToList();

            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

            var users = emailsWithFine.Where(x => x.FinePaid > 500).Select(x => x.User!).Distinct().ToList();

            if (users is not null && users.Any())
            {
                foreach (var user in users)
                {
                    user.AccountStatus = AccountStatus.BLOCKED;
                }
                contextBook.SaveChanges();

                return Ok("blocked");
            }
            else
            {
                return Ok("not blocked");
            }
        }

        [Authorize]
        [HttpGet("Unblock")]
        public ActionResult Unblock(int userId)
        {
            var user = contextBook.Users.Find(userId);
            if (user is not null)
            {
                user.AccountStatus = AccountStatus.ACTIVE;
                contextBook.SaveChanges();
                return Ok("unblocked");
            }

            return Ok("not unblocked");
        }
    }
}
