# --- 5 Minute Tests --- 
## "/" --> "/account" (50 users):
### Lightsail Instance Data:
  ![alt text](images/lightsail_cpu_5min.png)
### Locust Data:
  ![alt text](images/total_rqps_5_minutes.png)

## "/" --> "/account" (100 users):
### Lightsail Instance Data:
  ![alt text](images/lightsail_cpu_5min_100users.png)
### Locust Data:
  ![alt text](images/total_rqps_5min_100.png)


# Heavier workloads --- 5 Minute Tests --- 
## "/" --> "/recipes" --> "/recipes/view/2" --> "/dashboard" (50 users)
### Lightsail Instance data:
  ![alt text](images/lightsail_cpu_5min_recipe.png)
### Locust Data:
  ![alt text](images/total_rqps_5min_recipe.png)

## "/" --> "/recipes" --> "/recipes/view/2" --> "/dashboard" (100 users)

### Lightsail Instance Data:
![alt text](images/lightsail_cpu_5min_100users_recipe.png)

### Locust Data:
  ![alt text](images/total_rqps_5min_100users_recipe.png)


## Pushing the limits --- 200 Users --- 15 minutes ---

### Lightsail Instance Data:
  ![alt text](images/lightsail_cpu_200users_15min.png)

### Locust Data:
  ![alt text](images/total_rqps_15min_200users.png)