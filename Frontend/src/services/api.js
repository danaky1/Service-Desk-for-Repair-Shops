import axios from 'axios'

const api = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json'
    }
})

// Добавляем токен авторизации в заголовки
api.interceptors.request.use(config => {
    const token = localStorage.getItem('masterToken')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

// Обработчик ошибок
api.interceptors.response.use(
    response => response,
    error => {
        if (error.response?.status === 401) {
            // Если неавторизован, удаляем токен
            localStorage.removeItem('masterToken')
            localStorage.removeItem('master')
            window.location.reload()
        }
        console.error('API Error:', error.response?.data || error.message)
        return Promise.reject(error)
    }
)

export default {
    // Авторизация
    async login(username, password) {
        const response = await api.post('/auth/login', { username, password })
        if (response.data.success) {
            localStorage.setItem('masterToken', response.data.token)
            localStorage.setItem('master', JSON.stringify(response.data.master))
        }
        return response
    },
    
    async logout() {
        const response = await api.post('/auth/logout')
        localStorage.removeItem('masterToken')
        localStorage.removeItem('master')
        return response
    },
    
    async getCurrentMaster() {
        const response = await api.get('/auth/current')
        return response
    },
    
    // Заказы
    async getOrders() {
        const response = await api.get('/orders')
        return response
    },
    
    async getOrder(id) {
        const response = await api.get(`/orders/${id}`)
        return response
    },
    
    async createOrder(orderData) {
        const response = await api.post('/orders', orderData)
        return response
    },
    
    async updateOrderStatus(id, statusData) {
        const response = await api.patch(`/orders/${id}/status`, statusData)
        return response
    },
    
    async updateOrderCost(id, costData) {
        const response = await api.put(`/orders/${id}/cost`, costData)
        return response
    },
    
    async deleteOrder(id) {
        const response = await api.delete(`/orders/${id}`)
        return response
    },
    
    // Клиенты
    async getClients() {
        const response = await api.get('/clients')
        return response
    },
    
    async getClient(id) {
        const response = await api.get(`/clients/${id}`)
        return response
    },
    
    async createClient(clientData) {
        const response = await api.post('/clients', clientData)
        return response
    },
    
    async updateClient(id, clientData) {
        const response = await api.put(`/clients/${id}`, clientData)
        return response
    },
    
    async deleteClient(id) {
        const response = await api.delete(`/clients/${id}`)
        return response
    },
    
    // Мастера
    async getMasters() {
        const response = await api.get('/masters')
        return response
    },
    
    async getMaster(id) {
        const response = await api.get(`/masters/${id}`)
        return response
    },
    
    // Запчасти
    async getSpareParts() {
        const response = await api.get('/spareparts')
        return response
    },
    
    async getSparePart(id) {
        const response = await api.get(`/spareparts/${id}`)
        return response
    },
    
    async createSparePart(partData) {
        const response = await api.post('/spareparts', partData)
        return response
    },
    
    async updateSparePart(id, partData) {
        const response = await api.put(`/spareparts/${id}`, partData)
        return response
    },
    
    async useSparePart(id, quantity) {
        const response = await api.patch(`/spareparts/${id}/use`, { quantity })
        return response
    },
    
    async deleteSparePart(id) {
        const response = await api.delete(`/spareparts/${id}`)
        return response
    }
}