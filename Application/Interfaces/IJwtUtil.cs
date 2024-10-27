namespace Application.Utils;

public interface IJwtUtil
{
    string ParseJwt(string token);
}