pipeline {
    agent any

    environment {
        IMAGE_NAME = "fibrabank-app:v1"
        CONTAINER_NAME = "fibrabank-web"
        NETWORK_NAME = "fibra-network"
        DB_CONTAINER = "fibra-db"
        RABBIT_CONTAINER = "fibra-rabbit"
    }

    stages {
        stage('Temizle') {
            steps {
                script {
                    sh "docker stop ${CONTAINER_NAME} || true"
                    sh "docker rm ${CONTAINER_NAME} || true"
                }
            }
        }

        stage('Network') {
            steps {
                script {
                    echo "Network Bağlantıları Kontrol Ediliyor"
                    sh "docker network connect ${NETWORK_NAME} ${DB_CONTAINER} || true"
                    sh "docker network connect ${NETWORK_NAME} ${RABBIT_CONTAINER} || true"
                }
            }
        }

        stage('(Build)') {
            steps {
                script {
                    echo "Docker İmajı Oluşturuluyor"
                    sh "docker build -t ${IMAGE_NAME} -f FibaPlus_Bank/Dockerfile ."
                }
            }
        }

        stage('(Deploy)') {
            steps {
                script {
                    echo "Uygulama Ayağa Kaldırılıyor"
                    sh """
                        docker run -d \
                        --name ${CONTAINER_NAME} \
                        --network ${NETWORK_NAME} \
                        -p 7000:8080 \
                        -e "ConnectionStrings__DefaultConnection=Server=${DB_CONTAINER},1433;Database=FibaPlusBankDb;User Id=sa;Password=FibaBank2026;TrustServerCertificate=True;" \
                        ${IMAGE_NAME}
                    """
                }
            }
        }
    }

    post {
        success {
            echo 'İşlem Başarılı Uygulama http://localhost:7000 adresinde yayında.'
        }
        failure {
            echo 'Hata Verdi.'
        }
    }
}