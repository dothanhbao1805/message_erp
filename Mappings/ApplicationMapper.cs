
using AutoMapper;
using messenger.DTOs;
using messenger.Models;

namespace messenger.Mappings
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper()
        {
            CreateMap<Users, UsersDTO>();

            CreateMap<RegisterDTO, Users>()
               .ForMember(dest => dest.Password_Hash, opt => opt.Ignore())
               .ForMember(dest => dest.Id, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

        }
    }

}