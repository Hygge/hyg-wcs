import { defineStore } from 'pinia'
// import api from '@/api'
import { useAuthStore } from '@/store'
import {Info} from "@/api/user.js";

export const useUserStore = defineStore('user', {
    state: () => ({
        userInfo: null,
    }),
    getters: {
        userId() {
            return this.userInfo?.id
        },
        username() {
            return this.userInfo?.username
        },
        nickName() {
            return this.userInfo?.nickName
        },
        avatar() {
            return this.userInfo?.avatar
        },
        currentRole() {
            return this.userInfo?.currentRole || {}
        },
        roles() {
            return this.userInfo?.roles || []
        },
    },
    actions: {
        async getUserInfo() {
            try {
                const res = await Info()
                const { id, userName, nickName, profile, roles, currentRole } = res.data || {}
                this.userInfo = {
                    id,
                    username : userName,
                    avatar: profile?.avatar,
                    nickName: nickName,
                    gender: profile?.gender,
                    address: profile?.address,
                    email: profile?.email,
                    roles,
                    currentRole,
                }
                return Promise.resolve(res.data)
            } catch (error) {
                return Promise.reject(error)
            }
        },
        /*async switchCurrentRole(roleCode) {
            const { data } = await api.switchCurrentRole(roleCode)
            const authStore = useAuthStore()
            authStore.resetLoginState()
            await nextTick()
            authStore.setToken(data)
        },*/
        resetUser() {
            this.$reset()
        },
    },
    persist: {
        key: 'vue-antDesign-admin-user',
    },
})
