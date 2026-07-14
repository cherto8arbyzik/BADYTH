using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Core
{

public interface INavigationService
{
    bool TryFindPath(Vector3 start, Vector3 destination, List<Vector3> output);
}
}
