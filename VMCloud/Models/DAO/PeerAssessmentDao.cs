/*
*	Author:       xzy
*	Created:      2019/7/16 
*   Description:  数据库访问类 互评相关
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VMCloud.Utils;

namespace VMCloud.Models.DAO
{
    public class PeerAssessmentDao
    {       
        /*
        * Create By xzy
        *增加一条新的互评信息
        * 2019/7/18 add
        */
        public static void AddPeerAssessment(Peer_assessment peerassessment)
        {
            using (var dbContext = new DataModels())
            {
                dbContext.Peer_assessment.Add(peerassessment);
                dbContext.SaveChanges();
            }
        }
        /*
         * Create By xzy
         *修改互评信息
         * 2019/7/18 add
         */
        public static int ChangePeerAssessmentInfo(Peer_assessment newpa)
        {
            using (var dbContext = new DataModels())
            {
                Peer_assessment peerassessment = dbContext.Peer_assessment.Find(newpa.student_id,newpa.assessor_id, newpa.experiment_id);
                QuickCopy.Copy(newpa,ref peerassessment);
                return dbContext.SaveChanges();
            }
        }
        /*
        * Create By xzy
        *查看某实验全部互评信息
        * 2019/7/18 add
        */
        public static List<Peer_assessment> getPeerAssessmentByExpId(int expId)
        {

            using (var dbContext = new DataModels())
            {
                List<Peer_assessment> PeerAssessments = dbContext.Peer_assessment.Where(pa => pa.experiment_id == expId).ToList();

                return PeerAssessments;
            }
        }
        /*
        * Create By xzy
        *查看某学生该实验接受的全部互评信息
        * 2019/7/18 add
        */
        public static List<Peer_assessment> getPeerAssessmentByStuId(string stuId)
        {
            using (var dbContext = new DataModels())
            {
                List<Peer_assessment> PeerAssessments = dbContext.Peer_assessment.Where(pa => pa.student_id == stuId).ToList();
                return PeerAssessments;
            }
        }

        /*
        * Create By xzy
        *按照实验查找某人全部互评信息
        * 2019/7/25
        */
        public static List<Peer_assessment> getPeerAssessmentByExpId(string stuId,int expId)
        {
            using (var dbContext = new DataModels())
            {
                List<Peer_assessment> PeerAssessments = dbContext.Peer_assessment.Where(pa => pa.experiment_id == expId&& pa.student_id == stuId).ToList();
                return PeerAssessments;
            }
        }
        /*
        * Create By xzy
        *按照评分人查找某实验全部互评信息
        * 2019/7/25
        */
        public static List<Peer_assessment> getPeerAssessmentByAssessorId(string assessorId, int expId)
        {
            using (var dbContext = new DataModels())
            {
                List<Peer_assessment> PeerAssessments = dbContext.Peer_assessment.Where(pa => pa.assessor_id == assessorId&&pa.experiment_id==expId).ToList();
                return PeerAssessments;
            }
        }
        /*
       * Create By xzy
       *按照expId，stuId,assessorId查找某条互评信息
       * 2019/7/25
       */
        public static Peer_assessment getPeerAssessment(string stuId,string assessorId,int expId)
        {
            using (var dbContext = new DataModels())
            {
                var PeerAssessment = dbContext.Peer_assessment.Find(stuId, assessorId ,expId);
                return PeerAssessment;
            }
        }
        /*
        * Create By xzy
        *互评打分
        * 2019/7/25
        */
        public string modifySingleScoreWithReason(string stuId,string reviewerId, int expId, float grade, string reason)
        {
            using(var dbContext=new DataModels())
            {
                var pr = dbContext.Peer_assessment.Find(stuId,reviewerId,expId);
                pr.origin_score = grade;
                pr.reason = reason;
                dbContext.SaveChanges();
                return "success";
            }
            
        }
        /*
        * Create By xzy
        *互评打分
        * 2019/8/1
        */
        public string modifySingleScore(string stuId, string reviewerId, int expId, float grade)
        {
            using (var dbContext = new DataModels())
            {
                var pr = dbContext.Peer_assessment.Find(stuId, reviewerId, expId);
                pr.origin_score = grade;
                dbContext.SaveChanges();
                return "success";
            }

        }
        /*
        * Create By xzy
        *互评分数修正
        * 
        * 2019/7/25
        */
        public static string CorrectScore(string stuId,int expId)
        {
            using(var dbContext=new DataModels())
            {
                List<Peer_assessment> pa = dbContext.Peer_assessment.Where(p => p.assessor_id == stuId && p.experiment_id == expId).ToList();
                List<Assignment> assignments = new List<Assignment>();
                foreach (var temp_pa in pa)
                {
                    var assignment = dbContext.Assignments.Where(a => a.student_id == temp_pa.student_id && a.experiment_id == temp_pa.experiment_id).First();
                    assignments.Add(assignment);
                        
                }
                string standard_stuId = "";
                float standard_origin_score = 0;
                float standard_score = 0;
                float rate = 0;
                foreach(var temp_a in assignments)
                {
                    if (temp_a.is_standard == 2)
                    {
                        standard_score = (float)temp_a.score;
                        standard_stuId = temp_a.student_id;
                    }
                    //if (temp_a.is_standard == 1)
                    //{
                    //    standard_score=(float)temp_a.score;
                    //    standard_stuId = temp_a.student_id;
                    //}
                }
                foreach (var temp_pa in pa)
                {
                    if (temp_pa.student_id == standard_stuId)
                    {
                        if(temp_pa.origin_score==null|| temp_pa.origin_score == 0)
                        {
                            standard_origin_score = standard_score;
                        }
                        else
                        {
                            standard_origin_score = (float)temp_pa.origin_score;
                        }
                    }
                }
                if (standard_origin_score >= standard_score)
                {
                    rate = 0.1F;
                }
                else
                {
                    rate = -0.1F;
                }
                foreach (var temp_pa in pa)
                {
                    if (temp_pa.score != null && temp_pa.appeal_status == 2)
                    {
                        break;
                    }
                    if (temp_pa.origin_score != null)
                    {
                       
                        if (temp_pa.origin_score * (1 + rate / 2) >= 100)
                            temp_pa.score = 100;
                        else if (temp_pa.origin_score * (1 + rate / 2) <= 0)
                            temp_pa.score = 0;
                        else
                            temp_pa.score = temp_pa.origin_score * (1 + rate / 2);//修正公式
                    }
                    dbContext.SaveChanges();
                }
                return "success";
            }
           
        }
        /*
        * Create By xzy
        *互评分数结算
        * 2019/8/1
        */
        public static string ComputeFinalScore(string stuId,int expId)
        {
            using (var dbContext=new DataModels())
            {
                Assignment assignment = dbContext.Assignments.Where(a => a.student_id == stuId && a.experiment_id == expId).First();
                List<Peer_assessment> pa = dbContext.Peer_assessment.Where(p => p.student_id == stuId && p.experiment_id == expId).ToList();
                int cnt = 0;
                float res = 0;
                if (assignment.score == null)
                {
                    foreach (var temp_pa in pa)
                    {
                        if (temp_pa.score != null)
                        {
                            cnt++;
                            res += (float)temp_pa.score;
                        }
                    }
                    if (cnt != 0)
                    {
                        assignment.score = res / cnt;
                        dbContext.SaveChanges();
                        return assignment.score.ToString();
                    }
                    else return "评分数量为0";
                }
                else return assignment.score.ToString();

            }
        }
        /*
        * Create By xzy
        *提交申诉,只改其中一个
        * 2019/7/26
        */
        public static string AppealPeerAssessment(string stuId,string assessorId,int expId, int? state,string reason="")
        {
            using(var dbContext=new DataModels())
            {
                var Pa = dbContext.Peer_assessment.Where(pa => pa.student_id == stuId &&pa.assessor_id==assessorId&& pa.experiment_id == expId).First();
                Pa.appeal_status = state;
                Pa.appeal_reason = reason;
                dbContext.SaveChanges();
                return "success";
            }
        }

    }
}