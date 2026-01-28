// hooks/use-notification.ts
import { toast } from "sonner";

type NotificationOptions = {
    title: string;
    description?: string;
    duration?: number; // ms
};

export function useNotification() {
    const notify = ({
                        title,
                        description,
                        duration = 3000,
                    }: NotificationOptions) => {
        toast(title, {
            description,
            duration,
        });
    };

    return { notify };
}
