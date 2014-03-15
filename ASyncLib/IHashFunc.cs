using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASyncLib
{
    public interface IHashFunc
    {
        byte[] ComputeHash(byte[] buffer);
        byte[] ComputeHash(byte[] buffer, int offset, int count);
    }
}
