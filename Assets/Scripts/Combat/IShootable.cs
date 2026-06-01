/// <summary>
/// Algo a lo que las balas del escuadrón pueden hacer daño: zombies, jaulas de
/// supervivientes y barreras. Permite que Bullet trate a todos por igual.
/// </summary>
public interface IShootable
{
    void TakeHit(float damage);
}
