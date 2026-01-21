public interface IBuffReceiver
{
    void BuffEnter(IBuffParam buffParam);
    void BuffStay(IBuffParam buffParam);
    void BuffExit(IBuffParam buffParam);
}