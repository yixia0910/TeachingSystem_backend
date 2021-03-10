namespace VMCloud.Models.DAO
{
    public class SangforDao
    {
        public static SangforInfo getInfo(string vmName)
        {
            using (var dbContext = new DataModels())
            {
                SangforInfo info = dbContext.SangforInfos.Find(vmName);
                
                if(info != null)
                {
                    return info;
                }
            }
            return null;
        }
    }
}