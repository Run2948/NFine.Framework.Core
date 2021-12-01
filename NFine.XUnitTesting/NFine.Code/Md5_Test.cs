using System;
using System.Collections.Generic;
using System.Text;
using NFine.Code;
using Xunit;
using NFine.Code.Internal;

namespace NFine.XUnitTesting.NFine.Code
{
   public class Md5_Test
    {
        [Fact]
        public void Md5Test()
        {
            var result = EncryptProvider.Md5("aaaaa", MD5Length.L16);
            Assert.Equal(16, result.Length);
            Console.WriteLine(result);
        }
    }
}
