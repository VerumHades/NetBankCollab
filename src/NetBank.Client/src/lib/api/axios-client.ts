import axios from 'axios';

export const useAxiosClient = () => {
    return axios.create({
        baseURL: import.meta.env.VITE_API_BASE_URL,
        withCredentials: true,
    });
};
