namespace Compartilhado.Domain.Entities.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<Usuario?> ObterPorEmailAsync(string email);
        Task<int> InsertAsync(Usuario usuario);
    }
}
