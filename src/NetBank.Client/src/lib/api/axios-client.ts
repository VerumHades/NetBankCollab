import axios from 'axios';

export const useAxiosClient = () => {
    const client = axios.create({
        baseURL: import.meta.env.VITE_API_BASE_URL,
        withCredentials: true,
    });

    
    client.interceptors.response.use(
        (error) => {
            console.log(error.data)
            return Promise.reject(error);
        }
    );

    return client;
};
