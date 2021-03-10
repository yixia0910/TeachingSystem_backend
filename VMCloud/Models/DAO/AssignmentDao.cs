/*
*	Author:       xzy
*	Created:      2019/7/16 
*   Description:  数据库访问类 作业相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class AssignmentDao
    {
        /*
         * Create By xzy
         *增加一个新的作业
         * 2019/7/18 add
         */
        public static void AddAssignment(Assignment assignment)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Assignments.Add(assignment);
                dbContext.SaveChanges();
            }
        }
        /*
         * Create By xzy
         *根据作业id，查看作业信息
         * 
         */
        public static Assignment GetAssignmentById(int assignmentId)
        {
            using (var dbContext = new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Find(assignmentId);
                if (assignment != null)
                {
                    return assignment;
                }
            }
            return null;
        }
        /*
         * Create By xzy
         *根据作业id，删除作业
         * 
         */
        public static void DeleteAssignment(int assignmentId)
        {
            using (var dbContext = new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Find(assignmentId);
                if (assignment != null)
                {
                    dbContext.Assignments.Remove(assignment);
                    dbContext.SaveChanges();
                }
            }
        }
        /*
         * Create By xzy
         * 更改作业信息
         * 
         *2019/7/18 采用QuickCopy代替原有键值对替换
         */
        public static void ChangeAssignmentInfo(Assignment newAssignment)
        {
            using (var dbContext = new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Find(newAssignment.id);
                QuickCopy.Copy<Assignment>(newAssignment, ref assignment);
                dbContext.SaveChanges();
                /*if (assignmentNew != null)
                {
                    assignmentNew.id = assignment.id;
                    assignmentNew.student_id = assignment.student_id;
                    assignmentNew.experiment_id = assignment.experiment_id;
                    assignmentNew.submit_time = assignment.submit_time;
                    assignmentNew.file = assignment.file;
                    assignmentNew.is_standard = assignment.is_standard;
                    dbContext.SaveChanges();
                }*/
            }
        }
        /*
         * Create By xzy
         * 查看该学生提交的所有作业
         * 2019/7/18
         */
        public static List<Assignment> GetAssignmentsByStuId(string stuId)
        {
            using(var dbContext=new DataModels())
            {
                List<Assignment> assignments = dbContext.Assignments.Where(a => a.student_id == stuId).ToList();
                return assignments;
            }

        }
         /*
         * Create By xzy
         * 查看该实验提交的所有作业
         * 2019/7/18
         */
        public static List<Assignment> GetAssignmentsByExpId(int expId)
        {
            using(var dbContext=new DataModels())
            {
                List<Assignment> assignments = dbContext.Assignments.Where(a => a.experiment_id == expId).ToList();
                return assignments;
            }

        }
        /*
         * Create By xzy
         * 根据学生id和实验id查找具体某个作业
         * 2019/8.27
         */
        public static Assignment GetAssignmentsByStuIdAndExpId(string stuid,int expId)
        {
            using (var dbContext = new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Where(a => a.student_id==stuid&&a.experiment_id == expId).SingleOrDefault();
                return assignment;
            }

        }

        /*
        * Create By xzy
        * 查看该实验提交的作业数
        * 2019/7/18
        * 
        * 2019/7/19 直接从数据库层面获取count
        */
        public static int GetAssignmentsNumByExpId(int expId)
        {
            
            using (var dbContext = new DataModels())
            {
                // List<Assignment> assignments = dbContext.Assignments.Where(a => a.experiment_id == expId).ToList();
                // assignNum = assignments.Count();
                return dbContext.Assignments.Where(a => a.experiment_id == expId).Count();
            }

        }
        /*
        * Create By xzy
        * 查看某实验所有标准作业
        * 2019/7/18
        */
        public static List<Assignment> GetStandardAssignmentsByExpId(int expId)
        {
            using (var dbContext = new DataModels())
            {
                List<Assignment> assignments = dbContext.Assignments.Where(a => a.experiment_id == expId&&a.is_standard==2).ToList();
                return assignments;
            }

        }
        /*
        * Create By xzy
        * 查看某实验所有待评分作业
        * 2019/7/18
        */
        public static List<Assignment> GetAssignmentsToMarkByExpId(int expId)
        {
            using (var dbContext = new DataModels())
            {
                List<Assignment> assignments = dbContext.Assignments.Where(a => a.experiment_id == expId && a.is_standard >=1).ToList();
                return assignments;
            }

        }
        /*
        * Create By xzy
        * 随机抽取评分作业
        * 2019/7/18
        */
        public static List<Assignment> GenAssignmentsToMark(int expId)
        {
            using (var dbContext = new DataModels())
            {
                
                List<Assignment> assignments = dbContext.Assignments.Where(a=>a.experiment_id==expId).ToList();
                List<Assignment> returnList = new List<Assignment>();
                int assCount = assignments.Count();
                if (assCount < 6)
                {
                    return null;
                }
                else
                {
                    Random random = new Random(DateTime.Now.Millisecond);
                    int r = random.Next(0, assCount - 1);
                    int step = random.Next(1, assCount / 6);
                    int cnt = 0;
                    while (cnt < 6)
                    {
                        returnList.Add(assignments[r]);
                        assignments[r].is_standard = 1;
                        r = (r + step) % assCount;
                        cnt++;
                    }
                    dbContext.SaveChanges();
                    return returnList;
                }
            }

        }
        /*
        * Create By xzy
        * 修改作业互评状态
        * 2019/7/18
        * opt :0:不是标准作业 1:选中了的评分作业 2:评分作业中间部分的标准作业
        */
        public static void SetAssignmentStandard(Assignment standardAssignment, int opt)
        {
            using (var dbContext = new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Find(standardAssignment.id);
                assignment.is_standard = opt;
                dbContext.SaveChanges();

            }

        }
        /*
       * Create By xzy
       * 设置指定数量的标准作业
       * 2019/7/18
       */
        public static void SetStandardAssignment(int expId,int requireNum)
        {
            using (var dbContext = new DataModels())
            {

                List<Assignment> assignments = dbContext.Assignments.Where(a => a.experiment_id == expId && a.is_standard != 0).ToList();
                assignments = assignments.OrderByDescending(a => a.score).ToList();
                int allnum = assignments.Count();
                int index =(requireNum+1) / 2;
                int setNum = 0;
                foreach (var a in assignments)
                {
                    a.is_standard = 1;
                }
                dbContext.SaveChanges();
                while (setNum < requireNum)
                {
                    assignments[index].is_standard = 2;
                    setNum++;
                    index++;
                }
                dbContext.SaveChanges();
            }
        }
         /*
         * 标准作业设置完毕，分配互评作业
         * created by xzy
         * 2019/9/20
         */
        public static void AssignPeerAsssessment(int expid)
        {
            List<Assignment> inputList = GetAssignmentsByExpId(expid);
            int count = inputList.Count;
            Assignment[] copyArray = new Assignment[count];
            inputList.CopyTo(copyArray);
            List<Assignment> copyList = new List<Assignment>();
            copyList.AddRange(copyArray);
            List<Assignment> outputList = new List<Assignment>();
            Random rd = new Random(DateTime.Now.Millisecond);
            while (copyList.Count > 0)
            {
                int rdIndex = rd.Next(0, copyList.Count - 1);
                Assignment remove = copyList[rdIndex];
                copyList.Remove(remove);
                outputList.Add(remove);
            }
            using (var dbContext=new DataModels())
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 1; j < 4; j++)
                    {
                        Peer_assessment newPR = new Peer_assessment();
                        newPR.assessor_id = outputList[i].student_id;
                        newPR.student_id = outputList[(i + j) % count].student_id;
                        newPR.experiment_id = expid;
                        newPR.appeal_status = 0;
                        dbContext.Peer_assessment.Add(newPR);
                        dbContext.SaveChanges();
                    }
                    List<Assignment> standard = GetStandardAssignmentsByExpId(expid);
                    int rdi = rd.Next(0, 3);
                    int rdiTry = 0;
                    Peer_assessment standardPA = new Peer_assessment();
                    standardPA.assessor_id = outputList[i].student_id;
                    while (PeerAssessmentDao.getPeerAssessment(standard[rdi].student_id, outputList[i].student_id,expid) != null && rdiTry<4)
                    {
                        rdi = (rdi + 1) % 4;
                        rdiTry++;
                    }
                    if (PeerAssessmentDao.getPeerAssessment(standard[rdi].student_id, outputList[i].student_id, expid)==null)
                    {
                        standardPA.student_id = standard[rdi].student_id;
                        standardPA.experiment_id = expid;
                        standardPA.appeal_status = 0;
                        dbContext.Peer_assessment.Add(standardPA);
                        dbContext.SaveChanges();
                    }
                }
               


            }
        }

       /*
       * Create By xzy
       *为作业评分
       * 2019/7/25
       */
        public static void ModifyScore(string stuId,int expId,float grade)
        {
            using(var dbContext=new DataModels())
            {
                var assignment = dbContext.Assignments.Where(a => a.student_id == stuId && a.experiment_id == expId).ToList();
                Assignment assignmentToSet = assignment.First();
                assignmentToSet.score = grade;
                dbContext.SaveChanges();
            }
        }
        public static void ModifyScore(int id, float grade)
        {
            using (var dbContext = new DataModels())
            {
                var assignment = dbContext.Assignments.Find(id);                
                assignment.score = grade;
                dbContext.SaveChanges();
            }
        }

        public static void AddFile(File file)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Files.Add(file);
                dbContext.SaveChanges();
            }
        }

        public static File GetFileById(string fileId)
        {
            using (var dbContext = new DataModels())
            {
                return dbContext.Files.Find(fileId);
            }
        }

        public static void AlterFile(File newFile)
        {
            using (var dbContext = new DataModels())
            {
                File file = dbContext.Files.Find(newFile.id);
                QuickCopy.Copy<File>(newFile, ref file);
                dbContext.SaveChanges();
            }
        }

    }
}



