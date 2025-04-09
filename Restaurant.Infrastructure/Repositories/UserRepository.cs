// using Amazon.DynamoDBv2.DataModel;
// using Restaurant.Domain.Entities;
// using System.Threading.Tasks;
//
// namespace Restaurant.Infrastructure.Repositories
// {
//     public class UserRepository : IUserRepository
//     {
//         private readonly IDynamoDBContext _context;
//
//         public UserRepository(IDynamoDBContext context)
//         {
//             _context = context;
//         }
//
//         public async Task<User> GetUserByIdAsync(string id)
//         {
//             return await _context.LoadAsync<User>(id);
//         }
//
//         public async Task SaveUserAsync(User user)
//         {
//             await _context.SaveAsync(user);
//         }
//     }
// }
