/*
*	Author:       Xing Zhihuan
*   Email:        1@roycent.cn	
*	Created:      2019/7/11 17:16:23
*   Description:  数据库访问类 用户相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class UserDao
    {
        public static User GetUserById(string userId)
        {
            using (var dbContext = new DataModels())
            {
                User user = dbContext.Users.Find(userId);
                if(user != null)
                {
                    return user;
                }
            }
            return null;
        }

        public static User GetUserByNickName(string userName)
        {
            using (var dbContext = new DataModels())
            {
                List<string> slist=dbContext.Users.Where(u => u.nick_name==userName).Select(u => u.id).ToList();
                if (slist.Count()!=0)
                {
                    string id = slist.First();
                    User user = dbContext.Users.Find(id);
                    if (user != null)
                    {
                        return user;
                    }
                }
                else
                {
                    return null;
                }
            }
            return null;
        }


        /*
        * Create By zzw
        * 根据id，修改密码
        * 成功返回1，没有改变返回0
        */
        public static int ChangePwd(string userId,string passwdNew)
        {
            using (var dbContext = new DataModels())
            {
                User user = dbContext.Users.Find(userId);
                user.passwd = passwdNew;
                return dbContext.SaveChanges();
            }
        }
        /*
        * Create By zzw
        * 根据id，修改is_accept为1
        * 成功返回1，没有改变返回0
        */
        public static int ChangeIsAccept(string userId)
        {
            using (var dbContext = new DataModels())
            {
                User user = dbContext.Users.Find(userId);
                user.accept_time = DateTime.Now.ToString();
                user.is_accept = true;
                return dbContext.SaveChanges();
            }
        }
        /*
       * Create By xzy
       * 根据id，修改个人信息
       */
        public static void ChangeInfo(string userId, string name, string email)
        {
            using (var dbContext = new DataModels())
            {
               
                    User user = dbContext.Users.Find(userId);
                    user.name = name;
                    user.email = email;
                    dbContext.SaveChanges();   
                    
            }
        }

        /*
       * Create By jyf
       * 根据id，修改个人信息
       */
        public static int ChangeInfo(User toAdd)
        {
            using (var dbContext = new DataModels())
            {

                User user = dbContext.Users.Find(toAdd.id);
                QuickCopy.Copy(toAdd, ref user);
                return dbContext.SaveChanges();

            }
        }

        public static List<string> GetUsersInDepart(string departId)
        {
            using(var dbContext = new DataModels())
            {
                return dbContext.Users.Where(u => u.department_id == departId).Select(u => u.id).ToList();
            }
        }

        /// <summary>
        /// 依据身份获取用户
        /// </summary>
        /// <param name="role">1-学生，2-教师，3-院系管理员，4-超级管理员</param>
        /// <param name="depid">在role=1，2有用，代表所在学院</param>
        /// <returns></returns>
        public static List<User> GetUserByRole(int role, string depid = "")
        {
            using (var dbContext = new DataModels())
            {
                if (role < 3 && depid != "")
                {
                    return dbContext.Users.Where(u => u.role == role).Where(u => u.department_id == depid).ToList();
                }
                return dbContext.Users.Where(u => u.role == role).ToList();
            }
                
        }

        /// <summary>
        /// 批量添加用户
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        public static int AddUser(List<User> users)
        {
            using(var dbContext = new DataModels())
            {
                foreach(User u in users)
                {
                    u.passwd = HttpUtil.Encrypt(u.passwd, System.Text.Encoding.UTF8);
                }
                List<User> oldUsers = dbContext.Users.ToList();
                users = users.Where(u => !oldUsers.Where(o => o.id.Equals(u.id)).Any()).ToList();
                dbContext.Users.AddRange(users);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        public static int DeleteUser(List<User> users)
        {
            using(var dbContext = new DataModels())
            {
                users.ForEach(u => dbContext.Entry(u).State = System.Data.Entity.EntityState.Deleted);
                dbContext.Users.RemoveRange(users);
                return dbContext.SaveChanges();
            }
        }
        
        /// <summary>
        /// 按id集和找寻用户
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<User> FindUserByIds(List<string> ids)
        {
            using(var dbContext = new DataModels())
            {
                /**
                List<User> users = new List<User>();
                foreach (string id in ids)
                {
                    users.Add(dbContext.Users.Where(u => u.id == id).Single());
                }
                //var dd =
                **/
                List<User> users = dbContext.Users.ToList().Where(u => ids.Exists(id => id == u.id)).ToList();//竟然不行。。。
                return users;
            }
        }

        /// <summary>
        /// 添加助教到课程 Create By zzw
        /// </summary>
        /// <param name="ast"></param>
        /// <returns>成功返回1，没有改变返回0</returns>
        public static int AddAstToCourse(Assistant ast)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Assistants.Add(ast);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 删除助教到课程 Create By zzw
        /// </summary>
        /// <param name="ast"></param>
        /// <returns>成功返回1，没有改变返回0</returns>
        public static int DeleteAstFromCourse(Assistant ast)
        {
            using (var dbContext = new DataModels())
            {
                List<Assistant> assts = dbContext.Assistants.Where(a => a.course_id == ast.course_id && a.student_id == ast.student_id).ToList();
                if (assts.Count == 0) return 0;
                dbContext.Assistants.Remove(assts.Single());
                return dbContext.SaveChanges();
            }
        }
        /// <summary>
        /// 管理员添加学期 Create By zzw
        /// </summary>
        /// <param name="term"></param>
        /// <returns>成功返回1，没有改变返回0</returns>
        public static int AddTerm(Term term)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Terms.Add(term);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 管理员删除学期 Create By zzw
        /// </summary>
        /// <param name="term"></param>
        /// <returns>成功返回1，没有改变返回0</returns>
        public static int DeleteTerm(Term term)
        {
            using (var dbContext = new DataModels())
            {
                Term t = dbContext.Terms.Find(term.id);
                dbContext.Terms.Remove(t);
                return dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// 返回当前学期 Create By zzw
        /// </summary>
        /// <param></param>
        /// <returns>Term</returns>
        public static Term GetNowTerm()
        {
            using (var dbContext = new DataModels())
            {
                Term t = dbContext.Terms.OrderBy(term => term.id).ToList().Last();
                return t;
            }
        }
    }
    
}